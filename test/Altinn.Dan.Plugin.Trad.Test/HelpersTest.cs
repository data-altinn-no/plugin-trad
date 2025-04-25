using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Altinn.Dan.Plugin.Trad.Test
{
    [TestClass]
    public class HelpersTest
    {

        [TestMethod]
        public void TestShouldUpdate()
        {
            // Busy time
            Assert.IsTrue(Helpers.ShouldRunUpdate(GetNorwayTime(6)));
            Assert.IsTrue(Helpers.ShouldRunUpdate(GetNorwayTime(12, 30)));
            Assert.IsTrue(Helpers.ShouldRunUpdate(GetNorwayTime(17, 59)));
            
            // Every half hour
            Assert.IsTrue(Helpers.ShouldRunUpdate(GetNorwayTime(5)));
            Assert.IsTrue(Helpers.ShouldRunUpdate(GetNorwayTime(5, 30)));
            Assert.IsTrue(Helpers.ShouldRunUpdate(GetNorwayTime(5, 32)));
            Assert.IsTrue(Helpers.ShouldRunUpdate(GetNorwayTime(5, 1)));
            Assert.IsTrue(Helpers.ShouldRunUpdate(GetNorwayTime(5, 2)));
            Assert.IsTrue(Helpers.ShouldRunUpdate(GetNorwayTime(19, 30)));
            Assert.IsFalse(Helpers.ShouldRunUpdate(GetNorwayTime(19, 7)));
            Assert.IsFalse(Helpers.ShouldRunUpdate(GetNorwayTime(19, 19)));
            Assert.IsFalse(Helpers.ShouldRunUpdate(GetNorwayTime(19, 43)));

        }

        private DateTime GetNorwayTime(int hour, int minute = 0)
        {
            var zn = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            DateTimeOffset dateTimeOffset = new DateTimeOffset(new DateTime(2022, 1, 1, hour, minute, 0, DateTimeKind.Unspecified), zn.BaseUtcOffset);

            return dateTimeOffset.DateTime;
        }

    }
}
