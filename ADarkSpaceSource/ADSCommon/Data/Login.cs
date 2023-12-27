using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADSCommon.Data
{
    public record LoginData(string UserName, string Password);
    public record RegisterData(string UserName, string Password, string DisplayName, string Email, string ConfirmPassword);
}
