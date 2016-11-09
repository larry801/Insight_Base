﻿using System;
using System.Net;
using System.ServiceModel.Web;
using System.Text;
using System.Threading;
using Insight.Base.Common;
using Insight.Utils.Common;
using Insight.Utils.Entity;
using Insight.Utils.Server;

namespace Insight.Base.OAuth
{
    public class Compare
    {
        // 当前Web操作上下文
        private readonly WebOperationContext _Context = WebOperationContext.Current;

        // 用于验证的目标对象
        private AccessToken _Token;

        /// <summary>
        /// 验证结果
        /// </summary>
        public Result Result { get; } = new Result();

        /// <summary>
        /// 用于验证的基准对象
        /// </summary>
        public Session Basis { get; private set; }

        /// <summary>
        /// 构造方法，如Action不为空，则同时进行鉴权
        /// </summary>
        /// <param name="action">操作码，默认为空</param>
        /// <param name="limit">单位时间(秒)内限制调用，0：不限制</param>
        /// <param name="userid">用户ID</param>
        public Compare(string action = null, int limit = 0, Guid? userid = null)
        {
            if (!InitVerify(limit)) return;

            if (Basis.UserId == userid) action = null;

            Verify(action);
        }

        /// <summary>
        /// 构造方法
        /// 如account和LoginName一致，忽略鉴权
        /// </summary>
        /// <param name="action">操作码</param>
        /// <param name="account">登录账号</param>
        public Compare(string action, string account)
        {
            if (!InitVerify(0)) return;

            if (Basis.UserIsSame(account)) action = null;

            Verify(action);
        }

        /// <summary>
        /// 构造方法，用于获取Code
        /// </summary>
        /// <param name="token">传入参数</param>
        public Compare(AccessToken token)
        {
            var time = CallManage.LimitCall(3);
            if (time > 0)
            {
                Result.TooFrequent(time);
                return;
            }

            _Token = token;
            if (!FindBasis()) return;

            var obj = new {Basis.ID, Basis.Stamp};
            Result.Success(obj);
        }

        /// <summary>
        /// 构造方法，用于获取AccessToken
        /// </summary>
        /// <param name="token">传入参数</param>
        /// <param name="signature">用户签名</param>
        /// <param name="did">登录部门ID（可为空）</param>
        public Compare(AccessToken token, string signature, string did)
        {
            var time = CallManage.LimitCall(3);
            if (time > 0)
            {
                Result.TooFrequent(time);
                return;
            }

            var parse = new GuidParse(did);
            if (!parse.Successful)
            {
                Result.InvalidGuid();
                return;
            }

            _Token = token;
            if (!FindBasis()) return;

            // 验证用户签名
            if (!Basis.Verify(signature, 3))
            {
                Result.InvalidAuth();
                return;
            }

            if (DateTime.Now > Basis.FailureTime) Basis.InitSecret();

            if (DateTime.Now > Basis.ExpiryTime) Basis.Refresh();

            Basis.Online(parse.Result);
            Result.Success(Basis.CreatorKey());
        }

        /// <summary>
        /// 构造方法，用于刷新Token
        /// </summary>
        /// <param name="limit">限制访问秒数</param>
        public Compare(int limit)
        {
            if (!InitVerify(limit)) return;

            // 未超时
            var now = DateTime.Now;
            if (now < Basis.ExpiryTime)
            {
                Result.WithoutRefresh();
                return;
            }

            // 已失效
            if (now > Basis.FailureTime)
            {
                Result.Failured();
                return;
            }

            // 验证用户签名
            if (!Basis.Verify(_Token.Secret, 2))
            {
                Result.InvalidAuth();
                return;
            }

            Basis.Refresh();
            Result.Success(Basis.CreatorKey());
        }

        /// <summary>
        /// 初始化验证数据
        /// </summary>
        /// <param name="limit">限制访问秒数</param>
        private bool InitVerify(int limit)
        {
            var time = CallManage.LimitCall(limit);
            if (time > 0)
            {
                Result.TooFrequent(time);
                return false;
            }

            try
            {
                var headers = _Context.IncomingRequest.Headers;
                var auth = headers[HttpRequestHeader.Authorization];
                var buffer = Convert.FromBase64String(auth);
                var json = Encoding.UTF8.GetString(buffer);
                _Token = Util.Deserialize<AccessToken>(json);

                return FindBasis();
            }
            catch (Exception ex)
            {
                _Context.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                var msg = $"提取验证信息失败。\r\nException:{ex}";
                var ts = new ThreadStart(() => new Logger("500101", msg).Write());
                new Thread(ts).Start();
                return false;
            }
        }

        /// <summary>
        /// 找到Token对应的Session并检查是否正常(正常授权、未封禁、未锁定)
        /// </summary>
        /// <returns>bool Session是否正常</returns>
        private bool FindBasis()
        {
            Basis = Common.GetSession(_Token);
            if (Basis == null || Basis.ID > Common.MaxAuth) return false;

            if (!Basis.Validity)
            {
                Result.Disabled();
                return false;
            }

            // 检查是否验证签名失败超过5次
            if (!Basis.Ckeck()) return true;

            Result.AccountIsBlocked();
            return false;
        }

        /// <summary>
        /// 对Secret进行校验，返回验证结果
        /// </summary>
        /// <param name="action">操作码，默认为空</param>
        /// <returns>bool 是否通过验证</returns>
        private void Verify(string action = null)
        {
            var now = DateTime.Now;
            if (now > Basis.FailureTime.AddMinutes(10))
            {
                Result.Failured();
                return;
            }

            if (now > Basis.ExpiryTime.AddMinutes(10))
            {
                Result.Expired();
                return;
            }

            // 验证Secret
            if (!Basis.Verify(_Token.Secret, 1))
            {
                Result.InvalidAuth();
                return;
            }

            // 如action为空，立即返回；否则进行鉴权
            Result.Success();
            if (action == null) return;

            Guid aid;
            if (!Guid.TryParse(action, out aid))
            {
                Result.InvalidGuid();
                return;
            }

            // 根据传入的操作码进行鉴权
            var auth = new Authority(Basis.UserId, Basis.DeptId);
            if (auth.Identify(aid)) return;

            Result.Forbidden();
        }
    }
}