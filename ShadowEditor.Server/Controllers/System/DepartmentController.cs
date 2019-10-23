﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web;
using System.IO;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using ShadowEditor.Server.Base;
using ShadowEditor.Server.Helpers;
using ShadowEditor.Model.System;
using ShadowEditor.Server.CustomAttribute;

namespace ShadowEditor.Server.Controllers.System
{
    /// <summary>
    /// 组织机构控制器
    /// </summary>
    public class DepartmentController : ApiBase
    {
        /// <summary>
        /// 获取列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public JsonResult List()
        {
            var mongo = new MongoHelper();

            var filter = Builders<BsonDocument>.Filter.Empty;

            var docs = mongo.FindAll(Constant.DepartmentCollectionName).ToList();

            var list = new List<DepartmentModel>();

            foreach (var doc in docs)
            {
                list.Add(new DepartmentModel
                {
                    ID = doc["ID"].ToString(),
                    ParentID = doc["ParentID"].ToString(),
                    Name = doc["Name"].ToString(),
                    AdministratorID = doc["AdministratorID"].ToString()
                });
            }

            return Json(new
            {
                Code = 200,
                Msg = "Get Successfully!",
                Data = list
            });
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult Add(DepartmentEditModel model)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                return Json(new
                {
                    Code = 300,
                    Msg = "Name is not allowed to be empty."
                });
            }

            var mongo = new MongoHelper();

            var doc = new BsonDocument
            {
                ["ID"] = ObjectId.GenerateNewId(),
                ["ParentID"] = model.ParentID,
                ["Name"] = model.Name,
                ["AdministratorID"] = model.AdministratorID
            };

            mongo.InsertOne(Constant.DepartmentCollectionName, doc);

            return Json(new
            {
                Code = 200,
                Msg = "Saved successfully!"
            });
        }

        /// <summary>
        /// 编辑
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult Edit(UserEditModel model)
        {
            var objectId = ObjectId.GenerateNewId();

            if (!string.IsNullOrEmpty(model.ID) && !ObjectId.TryParse(model.ID, out objectId))
            {
                return Json(new
                {
                    Code = 300,
                    Msg = "ID is not allowed."
                });
            }

            if (string.IsNullOrEmpty(model.Username))
            {
                return Json(new
                {
                    Code = 300,
                    Msg = "Username is not allowed to be empty.",
                });
            }

            if (string.IsNullOrEmpty(model.Name))
            {
                return Json(new
                {
                    Code = 300,
                    Msg = "Name is not allowed to be empty."
                });
            }

            if (string.IsNullOrEmpty(model.RoleID))
            {
                model.RoleID = "";
            }

            var mongo = new MongoHelper();

            // 判断是否是系统内置用户
            var filter = Builders<BsonDocument>.Filter.Eq("ID", objectId);
            var doc = mongo.FindOne(Constant.UserCollectionName, filter);

            if (doc == null)
            {
                return Json(new
                {
                    Code = 300,
                    Msg = "The user is not existed."
                });
            }

            var userName = doc["Username"].ToString();

            if (userName == "admin")
            {
                return Json(new
                {
                    Code = 300,
                    Msg = "Modifying system built-in users is not allowed."
                });
            }

            // 判断用户名是否重复
            var filter1 = Builders<BsonDocument>.Filter.Ne("ID", objectId);
            var filter2 = Builders<BsonDocument>.Filter.Eq("Username", model.Username);
            filter = Builders<BsonDocument>.Filter.And(filter1, filter2);

            var count = mongo.Count(Constant.UserCollectionName, filter);

            if (count > 0)
            {
                return Json(new
                {
                    Code = 300,
                    Msg = "The username is already existed.",
                });
            }

            filter = Builders<BsonDocument>.Filter.Eq("ID", objectId);

            var update1 = Builders<BsonDocument>.Update.Set("Username", model.Username);
            var update2 = Builders<BsonDocument>.Update.Set("Name", model.Name);
            var update3 = Builders<BsonDocument>.Update.Set("RoleID", model.RoleID);
            var update4 = Builders<BsonDocument>.Update.Set("UpdateTime", DateTime.Now);

            var update = Builders<BsonDocument>.Update.Combine(update1, update2, update3, update4);

            mongo.UpdateOne(Constant.UserCollectionName, filter, update);

            return Json(new
            {
                Code = 200,
                Msg = "Saved successfully!"
            });
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult Delete(string ID)
        {
            var objectId = ObjectId.GenerateNewId();

            if (!string.IsNullOrEmpty(ID) && !ObjectId.TryParse(ID, out objectId))
            {
                return Json(new
                {
                    Code = 300,
                    Msg = "ID is not allowed."
                });
            }

            var mongo = new MongoHelper();

            var filter = Builders<BsonDocument>.Filter.Eq("ID", objectId);
            var doc = mongo.FindOne(Constant.UserCollectionName, filter);

            if (doc == null)
            {
                return Json(new
                {
                    Code = 300,
                    Msg = "The user is not existed."
                });
            }

            var userName = doc["Username"].ToString();

            if (userName == "admin")
            {
                return Json(new
                {
                    Code = 300,
                    Msg = "It is not allowed to delete system built-in users."
                });
            }

            var update = Builders<BsonDocument>.Update.Set("Status", -1);

            mongo.UpdateOne(Constant.UserCollectionName, filter, update);

            return Json(new
            {
                Code = 200,
                Msg = "Delete successfully!"
            });
        }
    }
}
