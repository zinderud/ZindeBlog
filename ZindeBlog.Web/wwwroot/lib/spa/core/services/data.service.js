"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
var http_1 = require("@angular/http");
var core_1 = require("@angular/core");
var Observable_1 = require("rxjs/Observable");
var notification_service_1 = require("../../core/services/notification.service");
require("rxjs/add/operator/do");
require("rxjs/add/operator/catch");
require("rxjs/add/operator/map");
var DataService = (function () {
    function DataService(http, notificationService) {
        this.http = http;
        this.notificationService = notificationService;
    }
    DataService.prototype.set = function (baseUri, pageSize) {
        this._baseUri = baseUri;
        this._pageSize = pageSize;
    };
    DataService.prototype.get = function (page) {
        var uri = this._baseUri + '/' + this._pageSize.toString();
        return this.http.get(uri)
            .map(this.extractData)
            .do(function (data) { return console.log('getData: ' + JSON.stringify(data)); })
            .catch(this.handleError);
    };
    DataService.prototype.getAll = function () {
        return this.http.get(this._baseUri)
            .map(this.extractData)
            .do(function (data) { return console.log('All: ' + JSON.stringify(data)); })
            .catch(this.handleError);
    };
    DataService.prototype.post = function (data, mapJson) {
        if (mapJson === void 0) { mapJson = true; }
        if (mapJson)
            return this.http.post(this._baseUri, data)
                .map(function (response) { return response.json(); }).catch(this.handleError);
        else
            return this.http.post(this._baseUri, data);
    };
    DataService.prototype.delete = function (id) {
        var _this = this;
        var headers = new http_1.Headers({ 'Content-Type': 'application/json' });
        var options = new http_1.RequestOptions({ headers: headers });
        return this.http.delete(this._baseUri + '/' + id.toString(), options)
            .map(function (response) { return response.json(); })
            .do(function (data) { return console.log('deleteData: ' + _this._baseUri + id.toString() + ' =' + JSON.stringify(data)); })
            .catch(this.handleError);
    };
    DataService.prototype.deleteResource = function (resource) {
        return this.http.delete(resource)
            .map(function (response) { return response.json(); });
    };
    DataService.prototype.createProduct = function (any, options) {
        any.id = undefined;
        return this.http.post(this._baseUri, any, options)
            .map(this.extractData)
            .do(function (data) { return console.log('createData: ' + JSON.stringify(data)); })
            .catch(this.handleError);
    };
    DataService.prototype.updateProduct = function (any, options) {
        var url = this._baseUri;
        return this.http.put(url, any, options)
            .map(function () { return any; })
            .do(function (data) { return console.log('updatedata: ' + JSON.stringify(data)); })
            .catch(this.handleError);
    };
    DataService.prototype.extractData = function (res) {
        var body = res.json();
        return body.data || {};
    };
    DataService.prototype.handleError = function (error) {
        // in a real world app, we may send the server to some remote logging infrastructure
        // instead of just logging it to the console
        console.error(error);
        this.notificationService.printErrorMessage(Observable_1.Observable.throw(error.json().error).error);
        return Observable_1.Observable.throw(error.json().error || 'Server error');
    };
    return DataService;
}());
DataService = __decorate([
    core_1.Injectable(),
    __metadata("design:paramtypes", [http_1.Http, notification_service_1.NotificationService])
], DataService);
exports.DataService = DataService;
