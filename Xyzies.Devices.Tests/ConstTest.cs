using System;
using System.Collections.Generic;
using System.Text;

namespace Xyzies.Devices.Tests
{
    public class ConstTest
    {
        public const string StaticToken = "d64ded6fd8db3fead6c90e600d85cccc02cd3e2dafcc29e8d1ade61263229d0b16b5a92ffa1bad1d6325e302461c7a69630c5c913ab47fb7e284dcabba1ac91e";

        public static readonly string DefaultCompanyName = "CompanyNameTest";
        public const string DefaultCompanyNameNotBindAnyBranch = "CompanyNameTestNotBindAnyBranch";
        public static readonly string DefaultBranchName = "BranchNameTest";
        public static readonly string Password = "Secret12345";
        public static readonly string DefaultSignInNamesTypeForEmail = "emailAddress";
        public static readonly string DefaultScopeHasNotAccess = "xyzies.identity.user.read.all";

        public class Role
        {
            public const string AccountAdmin = "AccountAdmin";
            public const string OperationAdmin = "OperationAdmin";
            public const string SystemAdmin = "SystemAdmin";
            public const string Manager = "Manager";
            public const string Supervisor = "Supervisor";

        }
    }
}
