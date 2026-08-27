using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace KahaGameCore.FontLocalization.Editor
{
    internal sealed class TmpFontTargetBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var missing = TmpFontTargetScanner.ScanAll(
                TmpFontTargetIgnoreRegistry.instance);
            if (missing.Count == 0)
            {
                return;
            }

            string details = string.Join(
                "\n",
                missing.Take(20).Select(result =>
                    $"- {result.AssetPath} :: {result.HierarchyPath}"));
            string remainder = missing.Count > 20
                ? $"\n...另有 {missing.Count - 20} 個項目。"
                : string.Empty;

            throw new BuildFailedException(
                $"Build 已中斷：找到 {missing.Count} 個沒有 LocalizedFontTarget、也沒有忽略紀錄的 TextMeshProUGUI。\n" +
                details +
                remainder +
                "\n請開啟 KahaGameCore > Font Localization > TMP Font Target Scanner 處理。");
        }
    }
}
