using System.Globalization;
using KahaGameCore.Common;
using NUnit.Framework;

namespace KahaGameCore.Tests
{
    public sealed class InvariantCultureBootstrapTests
    {
        [Test]
        public void Apply_MakesDecimalParsingIndependentOfOsLocale()
        {
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            CultureInfo originalDefaultCulture =
                CultureInfo.DefaultThreadCurrentCulture;
            try
            {
                // de-DE 拿 "." 當千分位，會把 "0.5" 靜默讀成 5；
                // fr-FR 兩種分隔符都不認，解析失敗落回 0。
                // 這段先確認危害真的存在，免得下面的斷言變成空跑。
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                Assert.That(
                    float.TryParse("0.5", out float beforeApply)
                    && beforeApply == 0.5f,
                    Is.False,
                    "de-DE 不應該正確解析 \"0.5\"，" +
                    "若這裡通過代表測試環境的 culture 沒有生效。");

                InvariantCultureBootstrap.Apply();

                Assert.That(float.TryParse("0.5", out float afterApply), Is.True);
                Assert.That(afterApply, Is.EqualTo(0.5f));
            }
            finally
            {
                CultureInfo.DefaultThreadCurrentCulture = originalDefaultCulture;
                CultureInfo.CurrentCulture = originalCulture;
            }
        }
    }
}
