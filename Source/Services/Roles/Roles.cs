﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.ServiceModel;
using Insight.WS.Base.Common;
using Insight.WS.Base.Common.Entity;

namespace Insight.WS.Base
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public partial class Roles : IRoles
    {
        public JsonResult AddRole(SYS_Role role, DataTable action, DataTable data, DataTable custom)
        {
            throw new NotImplementedException();
        }

        public JsonResult RemoveRole(string id)
        {
            throw new NotImplementedException();
        }

        public JsonResult EditRole(string id, SYS_Role obj, List<object> adl, List<object> ddl, List<object> cdl, DataTable adt, DataTable ddt, DataTable cdt)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取所有角色
        /// </summary>
        /// <returns>JsonResult</returns>
        public JsonResult GetAllRole()
        {
            const string action = "3BC74B61-6FA7-4827-A4EE-E1317BF97388";
            var verify = new SessionVerify();
            if (!verify.Compare(action)) return verify.Result;

            var list = GetRoles();
            return list.Any() ? verify.Result.Success(list) : verify.Result.NoContent();
        }

        public JsonResult AddRoleMember(string id, List<string> tids, List<string> gids, List<string> uids)
        {
            throw new NotImplementedException();
        }

        public JsonResult DeleteRoleMember(string id, int type)
        {
            throw new NotImplementedException();
        }

        public JsonResult GetRoleMember()
        {
            throw new NotImplementedException();
        }

        public JsonResult GetRoleUser()
        {
            throw new NotImplementedException();
        }

        public JsonResult GetMemberOfTitle(string id)
        {
            throw new NotImplementedException();
        }

        public JsonResult GetMemberOfGroup(string id)
        {
            throw new NotImplementedException();
        }

        public JsonResult GetMemberOfUser(string id)
        {
            throw new NotImplementedException();
        }

        public JsonResult GetRoleActions(string id)
        {
            throw new NotImplementedException();
        }

        public JsonResult GetRoleRelData(string id)
        {
            throw new NotImplementedException();
        }

        public JsonResult GetRoleModulePermit()
        {
            throw new NotImplementedException();
        }

        public JsonResult GetRoleActionPermit()
        {
            throw new NotImplementedException();
        }

        public JsonResult GetRoleDataPermit()
        {
            throw new NotImplementedException();
        }

    }
}
