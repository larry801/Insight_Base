//------------------------------------------------------------------------------
// <auto-generated>
//     此代码已从模板生成。
//
//     手动更改此文件可能导致应用程序出现意外的行为。
//     如果重新生成代码，将覆盖对此文件的手动更改。
// </auto-generated>
//------------------------------------------------------------------------------

namespace Insight.WS.Verify
{
    using System;
    using System.Collections.Generic;
    
    public partial class SYS_User
    {
        public System.Guid ID { get; set; }
        public long SN { get; set; }
        public string Name { get; set; }
        public string LoginName { get; set; }
        public string Password { get; set; }
        public string PayPassword { get; set; }
        public string OpenId { get; set; }
        public string Description { get; set; }
        public int Type { get; set; }
        public bool BuiltIn { get; set; }
        public bool Validity { get; set; }
        public Nullable<System.Guid> CreatorUserId { get; set; }
        public System.DateTime CreateTime { get; set; }
    }
}
