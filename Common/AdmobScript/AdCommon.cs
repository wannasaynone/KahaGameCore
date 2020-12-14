using UnityEngine;
using System;
using System.Text;
using System.Security.Cryptography;

public class AdCommon 
{
	private static string Md5Sum(string strToEncrypt)
	{
		UTF8Encoding ue = new UTF8Encoding();
		byte[] bytes = ue.GetBytes(strToEncrypt);
		MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
		byte[] hashBytes = md5.ComputeHash(bytes);

		string hashString = "";
		for (int i = 0; i < hashBytes.Length; i++) 
		{
			hashString += Convert.ToString(hashBytes[i], 16).PadLeft(2 , '0');
		}
		return hashString.PadLeft(32, '0');
	}

	public static string DeviceIdForAdmob
	{
		get
		{
			#if UNITY_EDITOR
			return SystemInfo.deviceUniqueIdentifier;
			#elif UNITY_ANDROID
			AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			AndroidJavaObject currentActivity = jc.GetStatic<AndroidJavaObject>("currentActivity");
			AndroidJavaObject contentResolver = currentActivity.Call<AndroidJavaObject>("getContentResolver");
			AndroidJavaObject secure = new AndroidJavaObject("android.provider.Settings$Secure");
			string deviceID = secure.CallStatic<string>("getString" , contentResolver, "android_id");
			return Md5Sum(deviceID).ToUpper();
			#elif UNITY_IOS
			return Md5Sum(UnityEngine.iOS.Device.advertisingIdentifier);
			#else
			return SystemInfo.deviceUniqueIdentifier;
			#endif
		}
	}
}