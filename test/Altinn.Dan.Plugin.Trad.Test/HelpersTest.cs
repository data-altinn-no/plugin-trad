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
            Assert.IsTrue(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: false, 5, 0)));
            Assert.IsTrue(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: false, 12, 30)));
            Assert.IsTrue(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: false, 17, 59)));

            // "Busy time" but weekend
            Assert.IsTrue(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: true, 5, 0)));
            Assert.IsFalse(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: true, 5, 10)));
            Assert.IsFalse(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: true, 12, 30)));
            Assert.IsFalse(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: true, 17, 52)));
            Assert.IsTrue(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: true, 17, 59)));

            // Hourly (evenings/nights or weekends)
            Assert.IsTrue(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: false, 4, 0)));
            Assert.IsFalse(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: false, 4, 30)));
            Assert.IsTrue(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: false, 5, 1)));
            Assert.IsTrue(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: false, 5, 2)));
            Assert.IsFalse(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: false, 19, 30)));
            Assert.IsTrue(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: false, 19, 7)));
            Assert.IsFalse(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: false, 19, 19)));
            Assert.IsFalse(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: false, 19, 43)));

            Assert.IsTrue(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: true, 4, 0)));
            Assert.IsFalse(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: true, 4, 30)));
            Assert.IsTrue(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: true, 5, 1)));
            Assert.IsTrue(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: true, 5, 2)));
            Assert.IsFalse(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: true, 19, 30)));
            Assert.IsTrue(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: true, 19, 7)));
            Assert.IsFalse(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: true, 19, 19)));
            Assert.IsFalse(Helpers.ShouldRunUpdate(GetNorwayTime(weekend: true, 19, 43)));

        }

        private DateTime GetNorwayTime(bool weekend, int hour, int minute = 0)
        {
            var zn = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            var day = weekend ? 1 : 3; // January 1st 2022 is saturday, 3rd is monday
            DateTimeOffset dateTimeOffset = new DateTimeOffset(new DateTime(2022, 1, day, hour, minute, 0, DateTimeKind.Unspecified), zn.BaseUtcOffset);

            return dateTimeOffset.DateTime;
        }

    }
}
