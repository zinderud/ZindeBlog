﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

using ZindeBlog.Web.Infrastructure.Core;
using ZindeBlog.Web.ViewModels.Setting;
using ZindeBlog.Web.ViewModels.Comment;
using ZindeBlog.Web.Entities.Blog;
using ZindeBlog.Web.Infrastructure.Extensions;
using ZindeBlog.Web.ViewModels.Topic;

namespace ZindeBlog.Web.Infrastructure.Services
{
    public class CommentService
    {
        private ZindeBlogContext BlogContext { get; set; }

        private ClientManager ClientManager { get; set; }

        private IMemoryCache Cache { get; set; }

        private IServiceProvider ServiceProvider { get; set; }

        private SettingModel Settings { get; set; }

        private EmailService EmailService { get; set; }

        public CommentService(ZindeBlogContext blogContext, ClientManager clientManager, IMemoryCache cache, IServiceProvider serviceProvider, SettingModel settings, EmailService emailService)
        {
            this.BlogContext = blogContext;
            this.ClientManager = clientManager;
            this.Cache = cache;
            this.ServiceProvider = serviceProvider;
            this.Settings = settings;
            this.EmailService = emailService;
        }

        public async Task<OperationResult<CommentModel>> Add(AddCommentModel model)
        {
            var topic = await BlogContext.Topics.SingleOrDefaultAsync(t => t.ID == model.TopicID);
            if (topic == null || topic.Status != Enums.TopicStatus.Published)
            {
                return OperationResult<CommentModel>.Failure("EmailService Failure");
            }

            var topicService = this.ServiceProvider.GetService<TopicService>();
            if (!topicService.CanComment(topic))
            {
                return OperationResult<CommentModel>.Failure("EmailService Add Failure");
            }

            Comment replyEntity = null;
            if (model.ReplyTo.HasValue)
            {
                replyEntity = await BlogContext.Comments.SingleOrDefaultAsync(t => t.ID == model.ReplyTo.Value);

                if (replyEntity == null || replyEntity.Status != Enums.CommentStatus.Approved)
                {
                    return OperationResult<CommentModel>.Failure("EmailService Add Failure");
                }

                if (replyEntity.TopicID != model.TopicID)
                {
                    return OperationResult<CommentModel>.Failure("EmailService Add Failure");
                }
            }

            Enums.CommentStatus status;
            if (this.Settings.VerifyComment)
            {
                status = Enums.CommentStatus.Pending;

                if (this.Settings.TrustAuthenticatedCommentUser)
                {
                    if (await BlogContext.Comments.AnyAsync(t => t.Email == model.Email && t.Status == Enums.CommentStatus.Approved))
                    {
                        status = Enums.CommentStatus.Approved;
                    }
                }
            }
            else
            {
                status = Enums.CommentStatus.Approved;
            }

            var entity = new Comment
            {
                Content = model.Content,
                CreateDate = DateTime.Now,
                CreateIP = this.ClientManager.ClientIP,
                Email = model.Email,
                Name = model.Name,
                NotifyOnComment = model.NotifyOnComment,
                ReplyToID = model.ReplyTo,
                TopicID = model.TopicID.Value,
                Status = status,
                UserID = this.ClientManager.CurrentUser?.Id,
                WebSite = model.WebSite
            };

            BlogContext.Comments.Add(entity);
            await BlogContext.SaveChangesAsync();

            await this.EmailService.SendCommentEmail(topic, entity);
            if (entity.Status == Enums.CommentStatus.Approved && entity.ReplyToID.HasValue && replyEntity.NotifyOnComment)
            {
                await this.EmailService.SendReplyEmail(topic, entity, replyEntity);
            }

            var commentModel = Transform(entity).First();

            return new OperationResult<CommentModel>(commentModel);
        }

        public async Task<OperationResult<CommentModel>> DirectlyReply(int replyTo, string content)
        {
            Comment comment = await BlogContext.Comments.SingleOrDefaultAsync(t => t.ID == replyTo);

            if (comment == null)
            {
                return OperationResult<CommentModel>.Failure("Comment DirectlyReply Failure ");
            }

            var entity = new Comment
            {
                Content = content,
                CreateDate = DateTime.Now,
                CreateIP = this.ClientManager.ClientIP,
                Email = this.ClientManager.CurrentUser.Email,
                Name = this.ClientManager.CurrentUser.Nickname,
                ReplyToID = replyTo,
                Status = Enums.CommentStatus.Approved,
                TopicID = comment.TopicID,
                UserID = this.ClientManager.CurrentUser.Id
            };

            this.BlogContext.Add(entity);
            await this.BlogContext.SaveChangesAsync();

            var commentModel = Transform(entity).First();
            return new OperationResult<CommentModel>(commentModel);
        }

        public async Task<List<CommentModel>> QueryByTopic(int topicID)
        {
            var query = BlogContext.Comments.AsNoTracking().Where(t => t.TopicID == topicID);
            if (ClientManager.IsLogin)
            {
                query = query.Where(t => t.Status == Enums.CommentStatus.Pending || t.Status == Enums.CommentStatus.Approved);
            }
            else
            {
                query = query.Where(t => t.Status == Enums.CommentStatus.Approved);
            }

            var entityList = await query.ToArrayAsync();

            return this.Transform(entityList);
        }

        public async Task<PagedResult<CommentModel>> Query(int pageIndex, int pageSize, Enums.CommentStatus? status, string keywords)
        {
            var query = this.BlogContext.Comments.AsNoTracking().AsQueryable();
            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }
            if (!string.IsNullOrWhiteSpace(keywords))
            {
                query = query.Where(t => t.Name.Contains(keywords) || t.Content.Contains(keywords) || t.WebSite.Contains(keywords) || t.Email.Contains(keywords));
            }

            int total = await query.CountAsync();

            query = query.OrderByDescending(t => t.ID)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize);

            var entityList = await query.ToArrayAsync();

            var modelList = this.Transform(entityList);

            return new PagedResult<CommentModel>(modelList, total);
        }

        public async Task BathUpdateStatus(int[] idList, Enums.CommentStatus status)
        {
            var entityList = await this.BlogContext.Comments.Where(t => idList.Contains(t.ID)).ToListAsync();
            foreach (var entity in entityList)
            {
                entity.Status = status;
            }

            await this.BlogContext.SaveChangesAsync();
        }

        public async Task BatchDelete(int[] idList)
        {
            int?[] deleteIDList = idList.Cast<int?>().ToArray();

            using (var tran = this.BlogContext.Database.BeginTransaction())
            {
                var entityList = await this.BlogContext.Comments.Where(t => deleteIDList.Contains(t.ID)).ToListAsync();
                this.BlogContext.RemoveRange(entityList);
                await this.BlogContext.SaveChangesAsync();

                var childReplyList = await this.BlogContext.Comments.Where(t => deleteIDList.Contains(t.ReplyToID)).ToListAsync();
                foreach (var entity in childReplyList)
                {
                    entity.ReplyToID = null;
                }
                await this.BlogContext.SaveChangesAsync();

                tran.Commit();
            }
        }

        public async Task<CommentModel> Delete(int id, bool deleteChild)
        {
            var entity = await this.BlogContext.Comments.SingleOrDefaultAsync(t => t.ID == id);
            if (entity == null)
            {
                return null;
            }

            var result = this.Transform(entity).First();

            this.BlogContext.Comments.Remove(entity);

            if (deleteChild)
            {
                var allCommentList = await this.BlogContext.Comments.Where(t => t.TopicID == entity.TopicID).ToListAsync();
                var idList = this.GetChildCommentIDList(allCommentList, entity.ID);

                var deleteEntityList = allCommentList.Where(t => idList.Contains(t.ID)).ToList();
                this.BlogContext.Comments.RemoveRange(deleteEntityList);
            }
            else
            {
                var replyEntityList = await this.BlogContext.Comments.Where(t => t.ReplyToID == entity.ID).ToListAsync();
                foreach (var replyEntity in replyEntityList)
                {
                    replyEntity.ReplyToID = null;
                }
            }

            await this.BlogContext.SaveChangesAsync();

            return result;
        }

        public async Task ApprovePendingComments(int topicID)
        {
            var entityList = await this.BlogContext.Comments.Where(t => t.TopicID == topicID && t.Status == Enums.CommentStatus.Pending).ToListAsync();

            foreach (var entity in entityList)
            {
                entity.Status = Enums.CommentStatus.Approved;
            }

            await this.BlogContext.SaveChangesAsync();
        }

        public async Task<CommentModel> ApproveComment(int id)
        {
            var entity = await this.BlogContext.Comments.SingleOrDefaultAsync(t => t.ID == id && t.Status == Enums.CommentStatus.Pending);

            if (entity == null)
            {
                return null;
            }

            entity.Status = Enums.CommentStatus.Approved;
            await this.BlogContext.SaveChangesAsync();

            return this.Transform(entity).First();
        }

        public async Task<List<CommentItemModel>> QueryLatest(int count)
        {
            string cacheKey = "Cache_Comment_Latest";
            var result = await this.Cache.RetriveCacheAsync(cacheKey, async () =>
            {
                var entityList = await this.BlogContext.Comments.AsNoTracking().Where(t => t.Status == Enums.CommentStatus.Approved)
                .OrderByDescending(t => t.ID)
                .Take(count)
                .ToListAsync();

                if (entityList.Count == 0)
                {
                    return new List<CommentItemModel>();
                }

                var topicIDList = entityList.Select(t => t.TopicID).ToArray();
                var topicList = await this.BlogContext.Topics
                    .Where(t => topicIDList.Contains(t.ID))
                    .Select(t => new TopicBasicModel
                    {
                        ID = t.ID,
                        Title = t.Title,
                        Alias = t.Alias
                    }).ToListAsync();
                var topicComments = await this.BlogContext.Comments.AsNoTracking().Where(t => topicIDList.Contains(t.TopicID))
                    .GroupBy(t => t.TopicID)
                    .Select(g => new
                    {
                        TopicID = g.Key,
                        Total = g.Count(),
                        Approved = g.Count(t => t.Status == Enums.CommentStatus.Approved),
                        Pending = g.Count(t => t.Status == Enums.CommentStatus.Pending),
                        Reject = g.Count(t => t.Status == Enums.CommentStatus.Reject)
                    }).ToListAsync();

                var modelList = entityList.Select(entity =>
                {
                    var commentModel = new CommentItemModel
                    {
                        ID = entity.ID,
                        Name = entity.Name,
                        Content = entity.Content
                    };

                    var topic = topicList.SingleOrDefault(t => t.ID == entity.TopicID);
                    commentModel.Topic = new TopicBasicModel
                    {
                        ID = topic.ID,
                        Title = topic.Title,
                        Alias = topic.Alias,
                        Comments = new CommentCountModel()
                    };

                    var commentCount = topicComments.SingleOrDefault(t => t.TopicID == entity.TopicID);
                    if (commentCount != null)
                    {
                        commentModel.Topic.Comments.Approved = commentCount.Approved;
                        commentModel.Topic.Comments.Reject = commentCount.Reject;
                        commentModel.Topic.Comments.Pending = commentCount.Pending;
                        commentModel.Topic.Comments.Total = commentCount.Total;
                    }

                    return commentModel;
                });

                return modelList.ToList();
            });

            return result;
        }

        private List<CommentModel> Transform(params Comment[] entityList)
        {
            var userIDList = entityList.Where(t => t.UserID.HasValue).Select(t => t.UserID.Value).ToList();
            var userList = this.BlogContext.QueryUserFromCache().Where(t => userIDList.Contains(t.Id)).ToList();

            var result = from comment in entityList
                         join user in userList on comment.UserID equals user.Id into u
                         from user in u.DefaultIfEmpty()
                         select new CommentModel
                         {
                             Content = comment.Content,
                             CreateDate = comment.CreateDate,
                             CreateIP = comment.CreateIP,
                             Email = comment.Email,
                             ID = comment.ID,
                             Name = comment.Name,
                             ReplyToID = comment.ReplyToID,
                             Status = comment.Status,
                             TopicID = comment.TopicID,
                             WebSite = comment.WebSite,
                             User = user != null ? new CommentModel.UserModel
                             {
                                 Nickname = user.Nickname,
                                 Email = user.Email,
                                 ID = user.Id,
                                 UserName = user.UserName
                             } : null
                         };

            return result.ToList();
        }

        private List<int> GetChildCommentIDList(List<Comment> entityList, int parent)
        {
            List<int> result = new List<int>();
            result.Add(parent);

            var children = entityList.Where(t => t.ReplyToID == parent).ToList();
            foreach (var child in children)
            {
                result.AddRange(this.GetChildCommentIDList(entityList, child.ID));
            }

            return result;
        }
    }
}
