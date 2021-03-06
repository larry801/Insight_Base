﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Insight.Base.Common;
using Insight.Base.Common.DTO;
using Insight.Base.Common.Entity;
using Insight.Base.OAuth;
using Insight.Utils.Common;
using Insight.Utils.Entity;

namespace Insight.Base.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class Organizations : ServiceBase, IOrganizations
    {
        /// <summary>
        /// 为跨域请求设置响应头信息
        /// </summary>
        public void responseOptions()
        {
        }

        /// <summary>
        /// 根据对象实体数据新增一个组织机构节点
        /// </summary>
        /// <param name="org">组织节点对象</param>
        /// <returns>Result</returns>
        public Result<object> addOrg(Org org)
        {
            if (!verify("newOrg")) return result;

            org.id = Util.newId();
            org.tenantId = tenantId;
            if (existed(org)) return result.dataAlreadyExists();

            org.creatorId = userId;
            org.createTime = DateTime.Now;

            return DbHelper.insert(org) ? result.created(org) : result.dataBaseError();
        }

        /// <summary>
        /// 根据ID删除组织机构节点
        /// </summary>
        /// <param name="id">节点ID</param>
        /// <returns>Result</returns>
        public Result<object> removeOrg(string id)
        {
            if (!verify("deleteOrg")) return result;

            var data = getData(id);
            if (data == null) return result.notFound();

            return DbHelper.delete(data) ? result : result.dataBaseError();
        }

        /// <summary>
        /// 根据对象实体数据更新组织机构信息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="org">组织节点对象</param>
        /// <returns>Result</returns>
        public Result<object> updateOrg(string id, Org org)
        {
            if (!verify("editOrg")) return result;

            var data = getData(org.id);
            if (data == null) return result.notFound();

            data.nodeType = org.nodeType;
            data.code = org.code;
            data.name = org.name;
            data.alias = org.alias;
            data.fullname = org.fullname;

            return DbHelper.update(data) ? result : result.dataBaseError();
        }

        /// <summary>
        /// 根据ID获取机构对象实体
        /// </summary>
        /// <param name="id">节点ID</param>
        /// <returns>Result</returns>
        public Result<object> getOrg(string id)
        {
            if (!verify("getOrgs")) return result;

            using (var context = new Entities())
            {
                var data = context.orgs.SingleOrDefault(i => !i.isInvalid && i.tenantId == tenantId && i.id == id);
                if (data == null) return result.notFound();

                var org = Util.convertTo<OrgDto>(data);
                var list = from m in context.orgMembers
                    join u in context.users on m.userId equals u.id
                    where m.orgId == id
                    select new MemberUser
                    {
                        id = m.id,
                        parentId = id,
                        name = u.name,
                        account = u.account,
                        remark = u.remark,
                        isInvalid = u.isInvalid
                    };
                org.members = list.ToList();

                return result.success(org);
            }
        }

        /// <summary>
        /// 获取组织机构树
        /// </summary>
        /// <returns>Result</returns>
        public Result<object> getOrgs()
        {
            if (!verify("getOrgs")) return result;

            using (var context = new Entities())
            {
                var list = context.orgs.Where(i => i.tenantId == tenantId).ToList();

                return list.Any() ? result.success(list) : result.noContent(new List<object>());
            }
        }

        /// <summary>
        /// 新增职位成员关系
        /// </summary>
        /// <param name="id"></param>
        /// <param name="members">成员集合</param>
        /// <returns>Result</returns>
        public Result<object> addOrgMember(string id, List<string> members)
        {
            if (!verify("addOrgMember")) return result;

            using (var context = new Entities())
            {
                var data = context.orgs.SingleOrDefault(i => !i.isInvalid && i.tenantId == tenantId && i.id == id);
                if (data == null) return result.notFound();

                data.members = new List<OrgMember>();
                members.ForEach(i =>
                {
                    var member = new OrgMember
                    {
                        id = Util.newId(),
                        orgId = id,
                        userId = i,
                        creatorId = userId,
                        createTime = DateTime.Now
                    };
                    data.members.Add(member);
                });
                if (!DbHelper.insert(data.members)) return result.dataBaseError();

                var list = from m in context.orgMembers
                    join u in context.users on m.userId equals u.id
                    where m.orgId == id
                    select new MemberUser
                    {
                        id = m.id,
                        parentId = id,
                        name = u.name,
                        account = u.account,
                        remark = u.remark,
                        isInvalid = u.isInvalid
                    };

                return result.success(list.ToList());
            }
        }

        /// <summary>
        /// 删除职位成员关系
        /// </summary>
        /// <param name="id"></param>
        /// <param name="members">成员集合</param>
        /// <returns>Result</returns>
        public Result<object> removeOrgMember(string id, List<string> members)
        {
            if (!verify("removeOrgMember")) return result;

            using (var context = new Entities())
            {
                var data = context.orgs.SingleOrDefault(i => !i.isInvalid && i.tenantId == tenantId && i.id == id);
                if (data == null) return result.notFound();

                data.members = context.orgMembers.Where(i => members.Any(m => m == i.id)).ToList();
                if (!DbHelper.delete(data.members)) return result.dataBaseError();

                var list = from m in context.orgMembers
                    join u in context.users on m.userId equals u.id
                    where m.orgId == id
                    select new MemberUser
                    {
                        id = m.id,
                        parentId = id,
                        name = u.name,
                        account = u.account,
                        remark = u.remark,
                        isInvalid = u.isInvalid
                    };

                return result.success(list.ToList());
            }
        }

        /// <summary>
        /// 获取职位成员之外的所有用户
        /// </summary>
        /// <param name="id">节点ID</param>
        public Result<object> getOtherOrgMember(string id)
        {
            if (!verify("getOrgs")) return result;

            using (var context = new Entities())
            {
                var list = from u in context.users
                    join r in context.tenantUsers on u.id equals r.userId
                    join m in context.orgMembers.Where(i => i.orgId == id) on u.id equals m.userId
                        into temp
                    from t in temp.DefaultIfEmpty()
                    where !u.isInvalid && r.tenantId == tenantId && t == null
                    orderby u.createTime
                    select new {u.id, u.name, u.account};
                return list.Any() ? result.success(list.ToList()) : result.noContent(new List<object>());
            }
        }

        /// <summary>
        /// 获取指定ID的用户组
        /// </summary>
        /// <param name="id">用户组ID</param>
        /// <returns>用户组</returns>
        private static Org getData(string id)
        {
            using (var context = new Entities())
            {
                return context.orgs.SingleOrDefault(i => i.id == id);
            }
        }

        /// <summary>
        /// 节点是否已存在
        /// </summary>
        /// <param name="org">用户组</param>
        /// <returns>是否已存在</returns>
        private static bool existed(Org org)
        {
            using (var context = new Entities())
            {
                return context.orgs.Any(i => i.id != org.id && i.tenantId == org.tenantId
                                                            && (i.parentId == org.parentId && i.name == org.name
                                                                || !string.IsNullOrEmpty(org.code) && i.code == org.code
                                                                || !string.IsNullOrEmpty(org.alias) &&
                                                                i.alias == org.alias
                                                                || !string.IsNullOrEmpty(org.fullname) &&
                                                                i.fullname == org.fullname));
            }
        }
    }
}