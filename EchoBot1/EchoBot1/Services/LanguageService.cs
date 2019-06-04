using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using EchoBot1.Resources;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;

namespace EchoBot1.Services
{
    public class LanguageService
    {
        public static readonly Dictionary<string, string> LanguageChoiceMap = new Dictionary<string, string>()
        {
            { "English", "en" },
            { "Español", "es" }
        };
    }
}
