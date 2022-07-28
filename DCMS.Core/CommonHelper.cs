﻿using DCMS.Core.ComponentModel;
using DCMS.Core.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace DCMS.Core
{
	/// <summary>
	/// 公共辅助方法
	/// </summary>
	public partial class CommonHelper
	{

		private static Random random = new Random();

		public static int Next(int num)
		{
			lock (random)
			{
				return random.Next(0, num);
			}
		}

		#region Fields

		//we use EmailValidator from FluentValidation. So let's keep them sync - https://github.com/JeremySkinner/FluentValidation/blob/master/src/FluentValidation/Validators/EmailValidator.cs
		private const string _emailExpression = @"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-||_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+([a-z]+|\d|-|\.{0,1}|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])?([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))$";

		private static readonly Regex _emailRegex;

		#endregion

		#region Ctor

		static CommonHelper()
		{
			_emailRegex = new Regex(_emailExpression, RegexOptions.IgnoreCase);
		}

		#endregion

		#region Methods


		public static string EnsureSubscriberEmailOrThrow(string email)
		{
			var output = EnsureNotNull(email);
			output = output.Trim();
			output = EnsureMaximumLength(output, 255);

			if (!IsValidEmail(output))
			{
				throw new DCMSException("Email is not valid.");
			}

			return output;
		}


		public static bool IsValidEmail(string email)
		{
			if (string.IsNullOrEmpty(email))
			{
				return false;
			}

			email = email.Trim();

			return _emailRegex.IsMatch(email);
		}


		public static bool IsValidIpAddress(string ipAddress)
		{
			return IPAddress.TryParse(ipAddress, out IPAddress _);
		}

		public static string GenerateRandomDigitCode(int length)
		{
			var random = new Random();
			var str = string.Empty;
			for (var i = 0; i < length; i++)
			{
				str = string.Concat(str, random.Next(10).ToString());
			}

			return str;
		}


		public static int GenerateRandomInteger(int min = 0, int max = int.MaxValue)
		{
			var randomNumberBuffer = new byte[10];
			new RNGCryptoServiceProvider().GetBytes(randomNumberBuffer);
			return new Random(BitConverter.ToInt32(randomNumberBuffer, 0)).Next(min, max);
		}

		public static string EnsureMaximumLength(string str, int maxLength, string postfix = null)
		{
			if (string.IsNullOrEmpty(str))
			{
				return str;
			}

			if (str.Length <= maxLength)
			{
				return str;
			}

			var pLen = postfix?.Length ?? 0;

			var result = str.Substring(0, maxLength - pLen);
			if (!string.IsNullOrEmpty(postfix))
			{
				result += postfix;
			}

			return result;
		}


		public static string EnsureNumericOnly(string str)
		{
			return string.IsNullOrEmpty(str) ? string.Empty : new string(str.Where(char.IsDigit).ToArray());
		}

		public static string EnsureNotNull(string str)
		{
			return str ?? string.Empty;
		}

		/// <summary>
		/// Indicates whether the specified strings are null or empty strings
		/// </summary>
		/// <param name="stringsToValidate">Array of strings to validate</param>
		/// <returns>Boolean</returns>
		public static bool AreNullOrEmpty(params string[] stringsToValidate)
		{
			return stringsToValidate.Any(string.IsNullOrEmpty);
		}


		public static bool ArraysEqual<T>(T[] a1, T[] a2)
		{
			//also see Enumerable.SequenceEqual(a1, a2);
			if (ReferenceEquals(a1, a2))
			{
				return true;
			}

			if (a1 == null || a2 == null)
			{
				return false;
			}

			if (a1.Length != a2.Length)
			{
				return false;
			}

			var comparer = EqualityComparer<T>.Default;
			return !a1.Where((t, i) => !comparer.Equals(t, a2[i])).Any();
		}


		public static void SetProperty(object instance, string propertyName, object value)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			if (propertyName == null)
			{
				throw new ArgumentNullException(nameof(propertyName));
			}

			var instanceType = instance.GetType();
			var pi = instanceType.GetProperty(propertyName);
			if (pi == null)
			{
				throw new DCMSException("No property '{0}' found on the instance of type '{1}'.", propertyName, instanceType);
			}

			if (!pi.CanWrite)
			{
				throw new DCMSException("The property '{0}' on the instance of type '{1}' does not have a setter.", propertyName, instanceType);
			}

			if (value != null && !value.GetType().IsAssignableFrom(pi.PropertyType))
			{
				value = To(value, pi.PropertyType);
			}

			pi.SetValue(instance, value, new object[0]);
		}


		public static object To(object value, Type destinationType)
		{
			return To(value, destinationType, CultureInfo.InvariantCulture);
		}


		public static object To(object value, Type destinationType, CultureInfo culture)
		{
			if (value == null)
			{
				return null;
			}

			var sourceType = value.GetType();

			var destinationConverter = TypeDescriptor.GetConverter(destinationType);
			if (destinationConverter.CanConvertFrom(value.GetType()))
			{
				return destinationConverter.ConvertFrom(null, culture, value);
			}

			var sourceConverter = TypeDescriptor.GetConverter(sourceType);
			if (sourceConverter.CanConvertTo(destinationType))
			{
				return sourceConverter.ConvertTo(null, culture, value, destinationType);
			}

			if (destinationType.IsEnum && value is int)
			{
				return Enum.ToObject(destinationType, (int)value);
			}

			if (!destinationType.IsInstanceOfType(value))
			{
				return Convert.ChangeType(value, destinationType, culture);
			}

			return value;
		}


		public static T To<T>(object value)
		{
			//return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
			return (T)To(value, typeof(T));
		}


		public static string ConvertEnum(string str)
		{
			if (string.IsNullOrEmpty(str))
			{
				return string.Empty;
			}

			var result = string.Empty;
			foreach (var c in str)
			{
				if (c.ToString() != c.ToString().ToLower())
				{
					result += " " + c.ToString();
				}
				else
				{
					result += c.ToString();
				}
			}

			//ensure no spaces (e.g. when the first letter is upper case)
			result = result.TrimStart();
			return result;
		}

		/// <summary>
		/// 默认中国 zh-CN
		/// </summary>
		public static void SetChinaCulture()
		{
			var culture = new CultureInfo("zh-CN");
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = culture;
		}


		public static int GetDifferenceInYears(DateTime startDate, DateTime endDate)
		{
			//source: http://stackoverflow.com/questions/9/how-do-i-calculate-someones-age-in-c
			//this assumes you are looking for the western idea of age and not using East Asian reckoning.
			var age = endDate.Year - startDate.Year;
			if (startDate > endDate.AddYears(-age))
			{
				age--;
			}

			return age;
		}


		public static object GetPrivateFieldValue(object target, string fieldName)
		{
			if (target == null)
			{
				throw new ArgumentNullException("target", "The assignment target cannot be null.");
			}

			if (string.IsNullOrEmpty(fieldName))
			{
				throw new ArgumentException("fieldName", "The field name cannot be null or empty.");
			}

			var t = target.GetType();
			FieldInfo fi = null;

			while (t != null)
			{
				fi = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

				if (fi != null)
				{
					break;
				}

				t = t.BaseType;
			}

			if (fi == null)
			{
				throw new Exception($"Field '{fieldName}' not found in type hierarchy.");
			}

			return fi.GetValue(target);
		}

		#endregion

		#region Properties
		public static IDCMSFileProvider DefaultFileProvider { get; set; }

		#endregion



		/// <summary>
		/// 计算密码强度
		/// </summary>
		/// <param name="password">密码字符串</param>
		/// <returns></returns>
		public class ChpsResult
		{
			public bool RSL { set; get; }
			public string MSG { set; get; }
		}

		public static ChpsResult PasswordStrength(string password)
		{
			if (password.Length < 8 || password.Length > 16)
			{
				return new ChpsResult() { RSL = false, MSG = "密码长度不符合，密码长度：8-16" };
			}

			Regex rgx = new Regex(@"^[0-9a-zA-Z\x21-\x7e]{8,16}$");
			if (!rgx.IsMatch(password))
			{
				return new ChpsResult() { RSL = false, MSG = "密码只能包含数字，字母和字符" };
			}

			//字符统计
			int iNum = 0, iLtt = 0, iSym = 0;
			foreach (char c in password)
			{
				if (c >= '0' && c <= '9') iNum++;
				else if (c >= 'a' && c <= 'z') iLtt++;
				else if (c >= 'A' && c <= 'Z') iLtt++;
				else iSym++;
			}
			if (iLtt == 0 && iSym == 0) return new ChpsResult() { RSL = false, MSG = "纯数字密码，请加入字符和字母" }; //纯数字密码
			if (iNum == 0 && iLtt == 0) return new ChpsResult() { RSL = false, MSG = "纯符号密码，请加入数字和字母" };  //纯符号密码
			if (iNum == 0 && iSym == 0) return new ChpsResult() { RSL = false, MSG = "纯字母密码，请加入字符和数字" }; //纯字母密码

			if (iLtt == 0) return new ChpsResult() { RSL = false, MSG = "数字和符号构成的密码，请加入字母" }; ; //数字和符号构成的密码
			if (iSym == 0) return new ChpsResult() { RSL = false, MSG = "数字和字母构成的密码，请加入字符" }; ; //数字和字母构成的密码
			if (iNum == 0) return new ChpsResult() { RSL = false, MSG = "字母和符号构成的密码，请加入数字" }; //字母和符号构成的密码
			return new ChpsResult() { RSL = true, MSG = "密码符合" };
		}


		///// <summary>
		///// 验证一个字符串是有效的e-mail格式
		///// </summary>
		///// <param name="email">Email</param>
		//public static bool IsValidEmail(string email)
		//{
		//    if (String.IsNullOrEmpty(email))
		//        return false;

		//    email = email.Trim();
		//    var result = Regex.IsMatch(email, "^(?:[\\w\\!\\#\\$\\%\\&\\'\\*\\+\\-\\/\\=\\?\\^\\`\\{\\|\\}\\~]+\\.)*[\\w\\!\\#\\$\\%\\&\\'\\*\\+\\-\\/\\=\\?\\^\\`\\{\\|\\}\\~]+@(?:(?:(?:[a-zA-Z0-9](?:[a-zA-Z0-9\\-](?!\\.)){0,61}[a-zA-Z0-9]?\\.)+[a-zA-Z0-9](?:[a-zA-Z0-9\\-](?!$)){0,61}[a-zA-Z0-9]?)|(?:\\[(?:(?:[01]?\\d{1,2}|2[0-4]\\d|25[0-5])\\.){3}(?:[01]?\\d{1,2}|2[0-4]\\d|25[0-5])\\]))$", RegexOptions.IgnoreCase);
		//    return result;
		//}

		/// <summary>
		/// 验证一个字符串是有效的手机号码格式
		/// </summary>
		/// <param name="email">mobileNumber</param>
		/// <returns></returns>
		public static bool IsValidMobileNumber(string mobileNumber)
		{
			if (string.IsNullOrEmpty(mobileNumber))
			{
				return false;
			}

			mobileNumber = mobileNumber.Trim();
			var result = Regex.IsMatch(mobileNumber, "^0?(1)[0-9]{10}$", RegexOptions.IgnoreCase);

			return result;
		}

		///// <summary>
		///// 随机生成的数字代码
		///// </summary>
		///// <param name="length">Length</param>
		///// <returns>Result string</returns>
		//public static string GenerateRandomDigitCode(int length)
		//{
		//    var random = new Random();
		//    string str = string.Empty;
		//    for (int i = 0; i < length; i++)
		//        str = String.Concat(str, random.Next(10).ToString());
		//    return str;
		//}


		/// <summary>
		/// 生成随机数字
		/// </summary>
		/// <param name="length">生成长度</param>
		/// <returns></returns>
		public static string GenerateNumber(int Length)
		{
			return Number(Length, false);
		}

		/// <summary>
		/// 生成随机数字
		/// </summary>
		/// <param name="Length">生成长度</param>
		/// <param name="Sleep">是否要在生成前将当前线程阻止以避免重复</param>
		/// <returns></returns>
		public static string Number(int Length, bool Sleep)
		{
			if (Sleep)
			{
				System.Threading.Thread.Sleep(3);
			}

			string result = "";
			System.Random random = new Random();
			for (int i = 0; i < Length; i++)
			{
				result += random.Next(10).ToString();
			}
			return result;
		}

		/// <summary>
		/// 生成随机字母与数字
		/// </summary>
		/// <param name="IntStr">生成长度</param>
		/// <returns></returns>
		public static string GenerateStr(int Length)
		{
			return GenerateStr(Length, false);
		}
		/// <summary>
		/// 生成随机字母与数字
		/// </summary>
		/// <param name="Length">生成长度</param>
		/// <param name="Sleep">是否要在生成前将当前线程阻止以避免重复</param>
		/// <returns></returns>
		public static string GenerateStr(int Length, bool Sleep)
		{
			if (Sleep)
			{
				System.Threading.Thread.Sleep(3);
			}

			char[] Pattern = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
			string result = "";
			int n = Pattern.Length;
			System.Random random = new Random(~unchecked((int)DateTime.Now.Ticks));
			for (int i = 0; i < Length; i++)
			{
				int rnd = random.Next(0, n);
				result += Pattern[rnd];
			}
			return result;
		}


		/// <summary>
		/// 生成随机纯字母随机数
		/// </summary>
		/// <param name="IntStr">生成长度</param>
		/// <returns></returns>
		public static string GenerateStrchar(int Length)
		{
			return Str_char(Length, false);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Length"></param>
		/// <returns></returns>
		public static string GenerateRegmutouSoft()
		{
			string tempstr = "";
			int[] numArray = new int[16];
			for (int m = 0; m < 16; m++)
			{
				if ((m > 0) && ((m % 4) == 0))
				{
					tempstr = tempstr + "-";
				}
				tempstr = tempstr + "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"[GenerateRandomInteger(0, 36)];
			}
			return tempstr;
		}

		/// <summary>
		/// 生成随机纯字母随机数
		/// </summary>
		/// <param name="Length">生成长度</param>
		/// <param name="Sleep">是否要在生成前将当前线程阻止以避免重复</param>
		/// <returns></returns>
		public static string Str_char(int Length, bool Sleep)
		{
			if (Sleep)
			{
				System.Threading.Thread.Sleep(3);
			}

			char[] Pattern = new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
			string result = "";
			int n = Pattern.Length;
			System.Random random = new Random(~unchecked((int)DateTime.Now.Ticks));
			for (int i = 0; i < Length; i++)
			{
				int rnd = random.Next(0, n);
				result += Pattern[rnd];
			}
			return result;
		}


		///// <summary>
		///// 返回一个基于整数随机数，在指定的范围
		///// </summary>
		///// <param name="min">Minimum number</param>
		///// <param name="max">Maximum number</param>
		///// <returns>Result</returns>
		//public static int GenerateRandomInteger(int min = 0, int max = 2147483647)
		//{
		//    var randomNumberBuffer = new byte[10];
		//    new RNGCryptoServiceProvider().GetBytes(randomNumberBuffer);
		//    return new Random(BitConverter.ToInt32(randomNumberBuffer, 0)).Next(min, max);
		//}

		///// <summary>
		///// 确保一个字符串不超过允许的最大长度
		///// </summary>
		//public static string EnsureMaximumLength(string str, int maxLength, string postfix = null)
		//{
		//    if (String.IsNullOrEmpty(str))
		//        return str;

		//    if (str.Length > maxLength)
		//    {
		//        var result = str.Substring(0, maxLength);
		//        if (!String.IsNullOrEmpty(postfix))
		//        {
		//            result += postfix;
		//        }
		//        return result;
		//    }
		//    else
		//    {
		//        return str;
		//    }
		//}

		/// <summary>
		/// 确保一个字符串最小和最大长度
		/// </summary>
		public static bool BetweenLength(string str, int minLength, int maxLength)
		{
			if (string.IsNullOrEmpty(str))
			{
				return false;
			}

			return (str.Length >= minLength && str.Length <= maxLength);
		}

		/// <summary>
		/// 确保一个字符串为用户名格式
		/// </summary>
		public static bool UserNameFormat(string str)
		{
			if (string.IsNullOrEmpty(str))
			{
				return false;
			}

			str = str.Trim();
			var result = Regex.IsMatch(str, "^[A-Za-z0-9_\\-\\u4e00-\\u9fa5]+$", RegexOptions.IgnoreCase);
			return result;
		}

		/// <summary>
		/// 确保一个字符串只包含数字
		/// </summary>
		public static bool FullNumber(string str)
		{
			if (string.IsNullOrEmpty(str))
			{
				return false;
			}

			str = str.Trim();
			var result = Regex.IsMatch(str, "^[0-9]+$", RegexOptions.IgnoreCase);
			return result;
		}

		///// <summary>
		///// 确保一个字符串只包含数值
		///// </summary>
		///// <param name="str">Input string</param>
		///// <returns>Input string with only numeric values, empty string if input is null/empty</returns>
		//public static string EnsureNumericOnly(string str)
		//{
		//    if (String.IsNullOrEmpty(str))
		//        return string.Empty;

		//    var result = new StringBuilder();
		//    foreach (char c in str)
		//    {
		//        if (Char.IsDigit(c))
		//            result.Append(c);
		//    }
		//    return result.ToString();
		//}

		///// <summary>
		///// 确保一个字符串不为null
		///// </summary>
		///// <param name="str">Input string</param>
		///// <returns>Result</returns>
		//public static string EnsureNotNull(string str)
		//{
		//    if (str == null)
		//        return string.Empty;

		//    return str;
		//}

		///// <summary>
		///// 表示指定的字符串是否是null或空字符串
		///// </summary>
		///// <param name="stringsToValidate">Array of strings to validate</param>
		///// <returns>Boolean</returns>
		//public static bool AreNullOrEmpty(params string[] stringsToValidate)
		//{
		//    bool result = false;
		//    Array.ForEach(stringsToValidate, str =>
		//    {
		//        if (string.IsNullOrEmpty(str)) result = true;
		//    });
		//    return result;
		//}


		//private static AspNetHostingPermissionLevel? _trustLevel = null;
		///// <summary>
		///// 查找正在运行的应用程序的信任级别 (http://blogs.msdn.com/dmitryr/archive/2007/01/23/finding-out-the-current-trust-level-in-asp-net.aspx)
		///// </summary>
		///// <returns>The current trust level.</returns>
		//public static AspNetHostingPermissionLevel GetTrustLevel()
		//{
		//    if (!_trustLevel.HasValue)
		//    {
		//        //set minimum
		//        _trustLevel = AspNetHostingPermissionLevel.None;

		//        //determine maximum
		//        foreach (AspNetHostingPermissionLevel trustLevel in
		//                new AspNetHostingPermissionLevel[] {
		//                        AspNetHostingPermissionLevel.Unrestricted,
		//                        AspNetHostingPermissionLevel.High,
		//                        AspNetHostingPermissionLevel.Medium,
		//                        AspNetHostingPermissionLevel.Low,
		//                        AspNetHostingPermissionLevel.Minimal
		//                    })
		//        {
		//            try
		//            {
		//                new AspNetHostingPermission(trustLevel).Demand();
		//                _trustLevel = trustLevel;
		//                break; //we've set the highest permission we can
		//            }
		//            catch (System.Security.SecurityException)
		//            {
		//                continue;
		//            }
		//        }
		//    }
		//    return _trustLevel.Value;
		//}

		///// <summary>
		///// Sets a property on an object to a valuae.
		///// </summary>
		///// <param name="instance">The object whose property to set.</param>
		///// <param name="propertyName">The name of the property to set.</param>
		///// <param name="value">The value to set the property to.</param>
		//public static void SetProperty(object instance, string propertyName, object value)
		//{
		//    if (instance == null) throw new ArgumentNullException("instance");
		//    if (propertyName == null) throw new ArgumentNullException("propertyName");

		//    Type instanceType = instance.GetType();
		//    PropertyInfo pi = instanceType.GetProperty(propertyName);
		//    if (pi == null)
		//        throw new DCMSException("No property '{0}' found on the instance of type '{1}'.", propertyName, instanceType);
		//    if (!pi.CanWrite)
		//        throw new DCMSException("The property '{0}' on the instance of type '{1}' does not have a setter.", propertyName, instanceType);
		//    if (value != null && !value.GetType().IsAssignableFrom(pi.PropertyType))
		//        value = To(value, pi.PropertyType);
		//    pi.SetValue(instance, value, new object[0]);
		//}

		public static TypeConverter GetDCMSCustomTypeConverter(Type type)
		{
			//we can't use the following code in order to register our custom type descriptors
			//TypeDescriptor.AddAttributes(typeof(List<int>), new TypeConverterAttribute(typeof(GenericListTypeConverter<int>)));
			//so we do it manually here

			if (type == typeof(List<int>))
			{
				return new GenericListTypeConverter<int>();
			}

			if (type == typeof(List<decimal>))
			{
				return new GenericListTypeConverter<decimal>();
			}

			if (type == typeof(List<string>))
			{
				return new GenericListTypeConverter<string>();
			}

			return TypeDescriptor.GetConverter(type);
		}

		///// <summary>
		///// Converts a value to a destination type.
		///// </summary>
		///// <param name="value">The value to convert.</param>
		///// <param name="destinationType">The type to convert the value to.</param>
		///// <returns>The converted value.</returns>
		//public static object To(object value, Type destinationType)
		//{
		//    return To(value, destinationType, CultureInfo.InvariantCulture);
		//}

		///// <summary>
		///// Converts a value to a destination type.
		///// </summary>
		///// <param name="value">The value to convert.</param>
		///// <param name="destinationType">The type to convert the value to.</param>
		///// <param name="culture">Culture</param>
		///// <returns>The converted value.</returns>
		//public static object To(object value, Type destinationType, CultureInfo culture)
		//{
		//    if (value != null)
		//    {
		//        var sourceType = value.GetType();

		//        TypeConverter destinationConverter = GetDCMSCustomTypeConverter(destinationType);
		//        TypeConverter sourceConverter = GetDCMSCustomTypeConverter(sourceType);
		//        if (destinationConverter != null && destinationConverter.CanConvertFrom(value.GetType()))
		//            return destinationConverter.ConvertFrom(null, culture, value);
		//        if (sourceConverter != null && sourceConverter.CanConvertTo(destinationType))
		//            return sourceConverter.ConvertTo(null, culture, value, destinationType);
		//        if (destinationType.IsEnum && value is int)
		//            return Enum.ToObject(destinationType, (int)value);
		//        if (!destinationType.IsAssignableFrom(value.GetType()))
		//            return Convert.ChangeType(value, destinationType, culture);
		//    }
		//    return value;
		//}

		///// <summary>
		///// Converts a value to a destination type.
		///// </summary>
		///// <param name="value">The value to convert.</param>
		///// <typeparam name="T">The type to convert the value to.</typeparam>
		///// <returns>The converted value.</returns>
		//public static T To<T>(object value)
		//{
		//    //return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
		//    return (T)To(value, typeof(T));
		//}

		///// <summary>
		///// Convert enum for front-end
		///// </summary>
		///// <param name="str">Input string</param>
		///// <returns>Converted string</returns>
		//public static string ConvertEnum(string str)
		//{
		//    string result = string.Empty;
		//    char[] letters = str.ToCharArray();
		//    foreach (char c in letters)
		//        if (c.ToString() != c.ToString().ToLower())
		//            result += " " + c.ToString();
		//        else
		//            result += c.ToString();
		//    return result;
		//}

		/// <summary>  
		/// GMT时间转成本地时间  
		/// </summary>  
		/// <param name="gmt">字符串形式的GMT时间</param>  
		/// <returns></returns>  
		public static DateTime GMT2Local(string gmt)
		{
			DateTime dt = DateTime.MinValue;
			try
			{
				string pattern = "";
				if (gmt.IndexOf("+0") != -1)
				{
					gmt = gmt.Replace("GMT", "");
					pattern = "ddd, dd MMM yyyy HH':'mm':'ss zzz";
				}
				if (gmt.ToUpper().IndexOf("GMT") != -1)
				{
					pattern = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";
				}
				if (pattern != "")
				{
					dt = DateTime.ParseExact(gmt, pattern,CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
					dt = dt.ToLocalTime();
				}
				else
				{
					dt = Convert.ToDateTime(gmt);
				}
			}
			catch
			{
			}
			return dt;
		}


		/// <summary>
		/// 获取本周第几天
		/// </summary>
		/// <param name="day">本周第几天</param>
		/// <returns></returns>
		public static DateTime GetThisWeekDate(int day)
		{
			int addDay = 0;

			int weeknow = Convert.ToInt32(DateTime.Now.DayOfWeek);
			if (weeknow == 0)
			{
				weeknow = 7;
			}

			if (weeknow > day)
			{
				switch (day)
				{
					case 1:
						addDay = (-1) * (weeknow - 1);
						break;
					case 2:
						addDay = (-1) * (weeknow - 2);
						break;
					case 3:
						addDay = (-1) * (weeknow - 3);
						break;
					case 4:
						addDay = (-1) * (weeknow - 4);
						break;
					case 5:
						addDay = (-1) * (weeknow - 5);
						break;
					case 6:
						addDay = (-1) * (weeknow - 6);
						break;
					case 7:
						addDay = (-1) * (weeknow - 7);
						break;
				}
			}
			else
			{
				addDay = day - weeknow;
			}
			return DateTime.Now.AddDays(addDay);
		}

		/// <summary>
		/// 获取当前日期和本周星期几相差的天数
		/// </summary>
		/// <param name="day">本周第几天</param>
		/// <returns></returns>
		public static int GetDifferDay(int day)
		{
			DateTime date = GetThisWeekDate(day);
			return DateTime.Now.Subtract(date).Days;
		}


		/// <summary>
		/// Html格式化JS
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public static string HtmlToJs(string source)
		{
			return string.Format("{0}", string.Join("", source.Replace("\\", "\\\\")
			.Replace("/", "\\/")
			.Replace("'", "\\'")
			.Replace("\"", "\\\"")
			.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
			));
		}

		public static string UnicodeToGB(string text)
		{
			System.Text.RegularExpressions.MatchCollection mc = System.Text.RegularExpressions.Regex.Matches(text, "\\\\u([\\w]{4})");
			if (mc != null && mc.Count > 0)
			{
				foreach (System.Text.RegularExpressions.Match m2 in mc)
				{
					string v = m2.Value;
					string word = v.Substring(2);
					byte[] codes = new byte[2];
					int code = Convert.ToInt32(word.Substring(0, 2), 16);
					int code2 = Convert.ToInt32(word.Substring(2), 16);
					codes[0] = (byte)code2;
					codes[1] = (byte)code;
					text = text.Replace(v, Encoding.Unicode.GetString(codes)).Replace("\\\\\\", "\\").Replace("\\\\", "\\");

				}
			}
			return (text.Replace("\\r\\n", "")).Replace("\\r", "");
		}


		/// <summary>
		/// 过滤字符串中的html代码
		/// </summary>
		/// <param name="Str"></param>
		/// <returns>返回过滤之后的字符串</returns>
		public static string LostHTML(string Str)
		{
			string Re_Str = "";
			if (Str != null)
			{
				if (Str != string.Empty)
				{
					string Pattern = "<\\/*[^<>]*>";
					Re_Str = Regex.Replace(Str, Pattern, "");
				}
			}
			return (Re_Str.Replace("\\r\\n", "")).Replace("\\r", "");
		}


		public static string Filter(string sInput)
		{
			if (sInput == null || sInput == "")
			{
				return null;
			}

			string sInput1 = sInput.ToLower();
			string output = sInput;
			string pattern = @"*|and|exec|insert|select|delete|update|count|master|truncate|declare|char(|mid(|chr(|'";
			if (Regex.Match(sInput1, Regex.Escape(pattern), RegexOptions.Compiled | RegexOptions.IgnoreCase).Success)
			{
				//throw new Exception("字符串中含有非法字符!");
			}
			else
			{
				output = output.Replace("'", "''");
			}
			return output;
		}


		public static string FilterHTML(string html)
		{
			if (html == null)
			{
				return "";
			}

			System.Text.RegularExpressions.Regex regex1 = new System.Text.RegularExpressions.Regex(@"<script[\s\S]+</script *>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
			System.Text.RegularExpressions.Regex regex2 = new System.Text.RegularExpressions.Regex(@" href *= *[\s\S]*script *:", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
			System.Text.RegularExpressions.Regex regex3 = new System.Text.RegularExpressions.Regex(@" on[\s\S]*=", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
			System.Text.RegularExpressions.Regex regex4 = new System.Text.RegularExpressions.Regex(@"<iframe[\s\S]+</iframe *>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
			System.Text.RegularExpressions.Regex regex5 = new System.Text.RegularExpressions.Regex(@"<frameset[\s\S]+</frameset *>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
			System.Text.RegularExpressions.Regex regex6 = new System.Text.RegularExpressions.Regex(@"\<img[^\>]+\>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
			System.Text.RegularExpressions.Regex regex7 = new System.Text.RegularExpressions.Regex(@"</p>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
			System.Text.RegularExpressions.Regex regex8 = new System.Text.RegularExpressions.Regex(@"<p>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
			System.Text.RegularExpressions.Regex regex9 = new System.Text.RegularExpressions.Regex(@"<[^>]*>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
			html = regex1.Replace(html, ""); //过滤<script></script>标记
			html = regex2.Replace(html, ""); //过滤href=javascript: (<A>) 属性
			html = regex3.Replace(html, " _disibledevent="); //过滤其它控件的on...事件
			html = regex4.Replace(html, ""); //过滤iframe
			html = regex5.Replace(html, ""); //过滤frameset
			html = regex6.Replace(html, ""); //过滤frameset
			html = regex7.Replace(html, ""); //过滤frameset
			html = regex8.Replace(html, ""); //过滤frameset
			html = regex9.Replace(html, "");
			html = html.Replace(" ", "");
			html = html.Replace("</strong>", "");
			html = html.Replace("<strong>", "");
			return html;
		}



		public static string Lost(string chr)
		{
			if (chr == null || chr == string.Empty)
			{
				return "";
			}
			else
			{
				chr = chr.Remove(chr.LastIndexOf(","));
				return chr;
			}
		}


		public static bool IsGuidByReg(string strSrc)
		{
			Regex reg = new Regex("^[A-F0-9]{8}(-[A-F0-9]{4}){3}-[A-F0-9]{12}$", RegexOptions.Compiled);
			return reg.IsMatch(strSrc);
		}

		public static void WriteFiles(string input, string fpath, Encoding encoding)
		{
			//using (FileStream fs = new FileStream(fname, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
			//允许覆盖
			using (FileStream fs = new FileStream(fpath, FileMode.Create, FileAccess.Write))
			{
				if (encoding == null)
				{
					throw new ArgumentNullException("encoding");
				}

				///根据上面创建的文件流创建写数据流
				StreamWriter w = new StreamWriter(fs);
				///设置写数据流的起始位置为文件流的末尾
				w.BaseStream.Seek(0, SeekOrigin.End);
				w.Write(input);
				///清空缓冲区内容，并把缓冲区内容写入基础流
				w.Flush();
				///关闭写数据流
				w.Close();
				//
				fs.Close();
			}
		}


		public static void WriteLog(string input, string fn)
		{
			///指定日志文件的目录
			string logPath = System.AppDomain.CurrentDomain.BaseDirectory + "\\log\\";
			if (!Directory.Exists(logPath))
			{
				Directory.CreateDirectory(logPath);
			}
			if (string.IsNullOrEmpty(fn))
			{
				fn = "RS";
			}

			string fname = logPath + "" + fn + "_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
			///定义文件信息对象
			//FileInfo finfo = new FileInfo(fname);
			///创建只写文件流
			using (FileStream fs = new FileStream(fname, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
			{
				///根据上面创建的文件流创建写数据流
				StreamWriter w = new StreamWriter(fs, Encoding.UTF8);
				///设置写数据流的起始位置为文件流的末尾
				w.BaseStream.Seek(0, SeekOrigin.End);
				//w.Write("【" + fn + "】");
				///写入当前系统时间并换行
				//w.Write("{0} {1} \r\n", DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString());
				///写入------------------------------------“并换行
				///写入日志内容并换行
				w.Write(input + "\r\n");
				///清空缓冲区内容，并把缓冲区内容写入基础流
				w.Flush();
				///关闭写数据流
				w.Close();

				fs.Close();
			}
		}

		/// <summary>
		/// 获取时间戳
		/// </summary>
		/// <returns></returns>
		public static string GetTimeStamp(System.DateTime time, int length = 13)
		{
			long ts = ConvertDateTimeToInt(time);
			return ts.ToString().Substring(0, length);
		}
		/// <summary>  
		/// 将c# DateTime时间格式转换为Unix时间戳格式  
		/// </summary>  
		/// <param name="time">时间</param>  
		/// <returns>long</returns>  
		public static long ConvertDateTimeToInt(System.DateTime time)
		{
			System.DateTime startTime = System.TimeZoneInfo.ConvertTimeToUtc(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
			//TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
			long t = (time.Ticks - startTime.Ticks) / 10000;   //除10000调整为13位      
			return t;
		}
		/// <summary>        
		/// 时间戳转为C#格式时间        
		/// </summary>        
		/// <param name=”timeStamp”></param>        
		/// <returns></returns>        
		public static DateTime ConvertStringToDateTime(string timeStamp)
		{
			DateTime dtStart = System.TimeZoneInfo.ConvertTimeToUtc(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
			long lTime = long.Parse(timeStamp + "0000");
			TimeSpan toNow = new TimeSpan(lTime);
			return dtStart.Add(toNow);
		}

		/// <summary>
		/// 时间戳转为C#格式时间10位
		/// </summary>
		/// <param name="timeStamp">Unix时间戳格式</param>
		/// <returns>C#格式时间</returns>
		public static DateTime GetDateTimeFrom1970Ticks(long curSeconds)
		{
			DateTime dtStart = System.TimeZoneInfo.ConvertTimeToUtc(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
			return dtStart.AddSeconds(curSeconds);
		}

		/// <summary>
		/// 验证时间戳
		/// </summary>
		/// <param name="time"></param>
		/// <param name="interval">差值（分钟）</param>
		/// <returns></returns>
		public static bool IsTime(long time, double interval)
		{
			DateTime dt = GetDateTimeFrom1970Ticks(time);
			//取现在时间
			DateTime dt1 = DateTime.Now.AddMinutes(interval);
			DateTime dt2 = DateTime.Now.AddMinutes(interval * -1);
			if (dt > dt2 && dt < dt1)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// 判断时间戳是否正确（验证前8位）
		/// </summary>
		/// <param name="time"></param>
		/// <returns></returns>
		public static bool IsTime(string time)
		{
			string str = GetTimeStamp(DateTime.Now, 8);
			if (str.Equals(time))
			{
				return true;
			}
			else
			{
				return false;
			}
		}


		/// <summary>
		///  将整型转成整型数组
		/// </summary>
		/// <example>10 转成 num[0]=1 num[1]=0 </example>
		/// <param name="showNumber">整型数字</param>
		/// <param name="realCount">返回的实际大小，即数组长度</param>
		/// <returns>整型数组</returns>
		public static int[] GetNumHash(int showNumber, ref int realCount)
		{
			int[] num_hash = new int[10];
			int index = 0;
			while (showNumber / 10 != 0)
			{
				num_hash[index] = (showNumber % 10);
				showNumber /= 10;
				index++;
			}
			num_hash[index] = showNumber;
			realCount = index + 1;
			return num_hash;
		}


		#region 生成单据号
		/// <summary>
		/// 生成单据号
		/// </summary>
		/// <param name="billType">单据类型</param>
		/// <param name="storeId">经销商id</param>
		/// <returns>类型+7+12</returns>
		public static string GetBillNumber(string billType, int storeId)
		{
			string stamp = GetTimeStamp(DateTime.Now, 12);
			int realCount = 0;
			var str = storeId.ToString();
			//7位经销商编号，支持百万家
			if (str.Length > 7)
			{
				var start = int.Parse(str.Substring(0, 7));
				var end = int.Parse(str.Substring(7, str.Length - 7));
				storeId = start + end;
			}
			var numArry = GetNumHash(storeId, ref realCount);
			int[] arry = new int[7];
			for (var i = 0; i < 7; i++)
			{
				if (realCount > i)
				{
					arry[i] = numArry[i];
				}
				else
				{
					arry[i] = 0;
				}
			}
			string billNumber = billType + "" + string.Join("", arry) + "" + stamp;
			return billNumber;
		}
		#endregion


		/// <summary>
		/// 获取枚举描述
		/// </summary>
		/// <param name="enumValue"></param>
		/// <returns></returns>
		public static string GetEnumDescription(Enum enumValue)
		{
			try
			{
				string value = enumValue.ToString();
				FieldInfo field = enumValue.GetType().GetField(value);
				//获取描述属性
				object[] objs = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
				//当描述属性没有时，直接返回名称
				if (objs == null || objs.Length == 0)
				{
					return value;
				}

				DescriptionAttribute descriptionAttribute = (DescriptionAttribute)objs[0];
				return descriptionAttribute.Description;
			}
			catch (Exception)
			{
				return enumValue.ToString();
			}
		}

		/// <summary>
		/// 将枚举转换成SelectListItem
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="textDefault">默认显示文字</param>
		/// <param name="valueDefault">默认值</param>
		/// <returns></returns>
		public static List<SelectListItem> EnumToSelectListItem<T>(string textDefault, string valueDefault = "0")
		{

			List<SelectListItem> selectListItems = new List<SelectListItem>();
			Type enumType = typeof(T);
			if (!enumType.IsEnum)
			{
				return selectListItems;
			}
			if (!string.IsNullOrEmpty(textDefault)) //判断是否添加默认选项
			{
				selectListItems.Add(new SelectListItem() { Text = textDefault, Value = valueDefault });
			}
			string[] fieldstrs = Enum.GetNames(enumType); //获取枚举字段数组
			foreach (var item in fieldstrs)
			{
				string description = string.Empty;
				var field = enumType.GetField(item);
				object[] arr = field.GetCustomAttributes(typeof(DescriptionAttribute), true); //获取属性字段数组
				if (arr != null && arr.Length > 0)
				{
					description = ((DescriptionAttribute)arr[0]).Description;   //属性描述
				}
				else
				{
					description = item;  //描述不存在取字段名称
				}
				selectListItems.Add(new SelectListItem()
				{
					Text = description,
					Value = ((int)Enum.Parse(enumType, item)).ToString()
				});
			}
			return selectListItems;

		}


		/// 获取枚举描述(泛型支持)
		/// </summary>
		/// <typeparam name="TEnum"></typeparam>
		/// <param name="enumValue"></param>
		/// <returns></returns>
		public static string GetEnumDescription<TEnum>(TEnum enumValue)
		{
			try
			{
				string value = enumValue.ToString();
				FieldInfo field = enumValue.GetType().GetField(value);
				//获取描述属性
				object[] objs = field?.GetCustomAttributes(typeof(DescriptionAttribute), false);
				//当描述属性没有时，直接返回名称
				if (objs == null || objs.Length == 0)
				{
					return value;
				}

				DescriptionAttribute descriptionAttribute = (DescriptionAttribute)objs[0];
				return descriptionAttribute.Description;
			}
			catch (Exception)
			{
				return enumValue.ToString();
			}
		}

		/// <summary>
		/// 获取枚举描述(泛型支持)
		/// </summary>
		/// <typeparam name="TEnum">枚举</typeparam>
		/// <param name="i">枚举具体值</param>
		/// <returns></returns>
		public static string GetEnumDescription<TEnum>(int i)
		{
			try
			{
				foreach (var item in Enum.GetValues(typeof(TEnum)))
				{
					if ((int)item == i)
					{
						return GetEnumDescription<TEnum>((TEnum)item);
					}
				}
				return "";
			}
			catch (Exception)
			{
				return "";
			}
		}


		/// <summary>
		/// 根据枚举值获取枚举(泛型支持)
		/// </summary>
		/// <typeparam name="TEnum">枚举</typeparam>
		/// <param name="i">枚举具体值</param>
		/// <returns></returns>
		public static TEnum GetEnumByValue<TEnum>(int value)
		{
			return (TEnum)Enum.Parse(typeof(TEnum), value.ToString());
		}



		/// <summary>
		/// 获取最小单位转换
		/// </summary>
		/// <param name="bigUnitIdint">大单位</param>
		/// <param name="strokeUnitId">中单位</param>
		/// <param name="smallUnitId">小单位</param>
		/// <param name="bigQuantity">大转小数量</param>
		/// <param name="strokeQuantity">中转小数量</param>
		/// <param name="thisUnitId">当前单位</param>
		/// <returns></returns>
		public static int GetSmallConversionQuantity(int bigUnitId, int strokeUnitId, int smallUnitId, int bigQuantity, int strokeQuantity, int thisUnitId)
		{
			int result = 1;
			if (thisUnitId == 0)
			{
				result = 1;
			}
			//大
			if (thisUnitId == bigUnitId)
			{
				result = bigQuantity;
			}
			else
			{
				//中
				if (thisUnitId == strokeQuantity)
				{
					result = strokeQuantity;
				}
				//小
				else
				{
					result = 1;
				}
			}

			if (result == 0)
			{
				result = 1;
			}
			return result;
		}




		/// <summary>
		/// 将获取的formData存入字典数组
		/// </summary>
		public static Dictionary<string, string> GetFormData(string formData, char splitChar = '&')
		{
			Dictionary<string, string> dataDic = new Dictionary<string, string>();
			try
			{
				//将参数存入字符数组
				string[] dataArry = formData?.Split(splitChar);
				//定义字典,将参数按照键值对存入字典中
				//遍历字符数组
				if (dataArry != null && dataArry.Length > 0)
				{
					for (int i = 0; i <= dataArry.Length - 1; i++)
					{
						//当前参数值
						string dataParm = dataArry[i];
						//"="的索引值
						int dIndex = dataParm.IndexOf("=");
						//参数名作为key
						string key = dataParm.Substring(0, dIndex);
						//参数值作为Value
						string value = dataParm.Substring(dIndex + 1, dataParm.Length - dIndex - 1);
						//将编码后的Value解码
						string deValue = System.Web.HttpUtility.UrlDecode(value, System.Text.Encoding.GetEncoding("utf-8"));
						if (key != "__VIEWSTATE")
						{
							//将参数以键值对存入字典
							dataDic.Add(key, deValue);
						}
					}
				}

				return dataDic;
			}
			catch (Exception)
			{
				return dataDic;
			}
		}

		public static string GetSqlUnitConversion(string productsTabelName)
		{
			//string sqlString = string.Format(@"(case when {0}.BigUnitId is not null and {0}.BigUnitId!=0
			//                                         then CONCAT('1',(select s1.`Name` from SpecificationAttributeOptions s1 where s1.Id = {0}.BigUnitId),' = ',{0}.BigQuantity,(select s1.`Name` from SpecificationAttributeOptions s1 where s1.Id = {0}.SmallUnitId))
			//                                   when {0}.StrokeUnitId is not null and {0}.StrokeUnitId != 0
			//                                         then CONCAT('1',(select s1.`Name` from SpecificationAttributeOptions s1 where s1.Id = {0}.StrokeUnitId),' = ',{0}.BigQuantity,(select s1.`Name` from SpecificationAttributeOptions s1 where s1.Id = {0}.SmallUnitId))
			//                                   else CONCAT('1', (select s1.`Name` from SpecificationAttributeOptions s1 where s1.Id = {0}.SmallUnitId),' = ','1',(select s1.`Name` from SpecificationAttributeOptions s1 where s1.Id = {0}.SmallUnitId)) end)", productsTabelName);

			string sqlString = $@"(case when {productsTabelName}.BigUnitId is not null and {productsTabelName}.BigUnitId!=0
													 then CONCAT('1',(select s1.`Name` from SpecificationAttributeOptions s1 where s1.Id = {productsTabelName}.BigUnitId),' = ',{productsTabelName}.BigQuantity,(select s1.`Name` from SpecificationAttributeOptions s1 where s1.Id = {productsTabelName}.SmallUnitId))
													 when {productsTabelName}.StrokeUnitId is not null and {productsTabelName}.StrokeUnitId != 0
													 then CONCAT('1',(select s1.`Name` from SpecificationAttributeOptions s1 where s1.Id = {productsTabelName}.StrokeUnitId),' = ',{productsTabelName}.BigQuantity,(select s1.`Name` from SpecificationAttributeOptions s1 where s1.Id = {productsTabelName}.SmallUnitId))
													 else CONCAT('1', (select s1.`Name` from SpecificationAttributeOptions s1 where s1.Id = {productsTabelName}.SmallUnitId),' = ','1',(select s1.`Name` from SpecificationAttributeOptions s1 where s1.Id = {productsTabelName}.SmallUnitId)) end)";
			return sqlString;

		}



		/// <summary>
		/// MD5加密字符串（32位大写）
		/// </summary>
		/// <param name="source">源字符串</param>
		/// <returns>加密后的字符串</returns>
		public static string MD5(string source)
		{
			MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
			byte[] bytes = Encoding.UTF8.GetBytes(source);
			string result = BitConverter.ToString(md5.ComputeHash(bytes));
			return result.Replace("-", "");
		}


		/// <summary>
		/// 根据科目的类型，判断余额方向是借方或者贷方
		/// </summary>
		/// <param name="accountingType"></param>
		/// <returns></returns>
		public static DirectionsTypeEnum GetAccountingDirections(int accountingType)
		{
			switch (accountingType)
			{
				//资产类 月末余额方向在“借”方
				case (int)AccountingEnum.Assets:
					return DirectionsTypeEnum.IN;
				//负债类 月末余额方向在“贷”方
				case (int)AccountingEnum.Liability:
					return DirectionsTypeEnum.OUT;
				//所有者权益类 月末余额方向在“贷”方
				case (int)AccountingEnum.Rights:
					return DirectionsTypeEnum.OUT;
				//收入类 月末余额方向在“贷”方
				case (int)AccountingEnum.Income:
					return DirectionsTypeEnum.OUT;
				//支出类 月末余额方向在“借”方
				case (int)AccountingEnum.Expense:
					return DirectionsTypeEnum.IN;
				default:
					return DirectionsTypeEnum.BALANCE;
			}
		}

		
		public static bool SendSMS(string url, string uid, string password, string content, string mobile)
		{
			try
			{
				//string url = "http://119.90.36.56:8090/jtdsms/smsSend.do";
				//string uid = "221423";
				//string password = "8765421";
				string encode = "utf8";
				string encodeType = "base64";
				string pswdMd5 = md5(password);
				Encoding unicode = Encoding.Unicode;
				Encoding utf_8 = Encoding.GetEncoding("UTF-8");
				byte[] unicodeBytes = unicode.GetBytes(content);
				byte[] utf8Bytes = Encoding.Convert(unicode, utf_8, unicodeBytes);
				UTF8Encoding utf8 = new UTF8Encoding();
				string contentToSend = Convert.ToBase64String(utf8Bytes);

				IDictionary<string, string> parameters = new Dictionary<string, string>();
				parameters.Add("uid", uid);
				parameters.Add("password", pswdMd5);
				parameters.Add("mobile", mobile);
				parameters.Add("encode", encode);
				parameters.Add("content", contentToSend);
				parameters.Add("encodeType", encodeType);


				HttpWebResponse res = CreatePostHttpResponse(url, parameters);
				return true;
			}
			catch (Exception ex)
			{
				return false;
			}
		}

		/// <summary>
		/// 发送短信加密
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static String md5(String s)
		{
			MD5 md5 = new MD5CryptoServiceProvider();
			byte[] bytes = System.Text.Encoding.UTF8.GetBytes(s);
			bytes = md5.ComputeHash(bytes);
			md5.Clear();

			string ret = "";
			for (int i = 0; i < bytes.Length; i++)
			{
				ret += Convert.ToString(bytes[i], 16).PadLeft(2, '0');
			}

			return ret.PadLeft(32, '0').ToUpper();
		}

		/// <summary>
		/// 创建Http请求
		/// </summary>
		/// <param name="url"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static HttpWebResponse CreatePostHttpResponse(string url, IDictionary<string, string> parameters)
		{
			HttpWebRequest request = null;
			try
			{
				//如果是发送HTTPS请求  
				if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
				{
					request = WebRequest.Create(url) as HttpWebRequest;
				}
				else
				{
					request = WebRequest.Create(url) as HttpWebRequest;
				}
				request.Method = "POST";
				request.ContentType = "application/x-www-form-urlencoded";



				//发送POST数据  
				if (!(parameters == null || parameters.Count == 0))
				{
					StringBuilder buffer = new StringBuilder();
					int i = 0;
					foreach (string key in parameters.Keys)
					{
						if (i > 0)
						{
							buffer.AppendFormat("&{0}={1}", key, parameters[key]);
						}
						else
						{
							buffer.AppendFormat("{0}={1}", key, parameters[key]);
							i++;
						}
					}
					byte[] data = Encoding.ASCII.GetBytes(buffer.ToString());
					using (Stream stream = request.GetRequestStream())
					{
						stream.Write(data, 0, data.Length);
					}
				}
				string[] values = request.Headers.GetValues("Content-Type");

				return request.GetResponse() as HttpWebResponse;
			}
			catch
			{
				return null;
			}
			finally
			{
				if (request != null)
				{
					request.Abort();
				}
			}
		}

		public static string GetWeek(string dt)
		{
			string week = "";
			//根据取得的英文单词返回汉字
			switch (dt)
			{
				case "Monday":
					week = "星期一";
					break;
				case "Tuesday":
					week = "星期二";
					break;
				case "Wednesday":
					week = "星期三";
					break;
				case "Thursday":
					week = "星期四";
					break;
				case "Friday":
					week = "星期五";
					break;
				case "Saturday":
					week = "星期六";
					break;
				case "Sunday":
					week = "星期日";
					break;
			}
			return week;
		}

		public static bool SendMail(string subject, string content, bool mock)
		{
			var attachmentPath = "";
			try
			{
				if (!string.IsNullOrEmpty(subject) && !string.IsNullOrEmpty(content))
				{
					var email = new MailMessage();
					email.From = new MailAddress("czhcom@163.com", "DCMS.Pusher");
		
					email.To.Add(new MailAddress("czhcom@163.com"));
					email.To.Add(new MailAddress("xmgswe@126.com"));
					email.To.Add(new MailAddress("duzhuang2012@163.com"));

					email.ReplyToList.Add(new MailAddress("czhcom@163.com"));
					email.Subject = subject;
					email.Body = content;
					if (!string.IsNullOrEmpty(attachmentPath))
					{
						var attachment = new Attachment(attachmentPath);
						email.Attachments.Add(attachment);
					}
					email.IsBodyHtml = true;
					email.Priority = MailPriority.Normal;
					var stmp = new SmtpClient("smtp.163.com", 25);
					stmp.UseDefaultCredentials = true;
					stmp.Credentials = new NetworkCredential("czhcom@163.com", "racing.1");
					stmp.EnableSsl = false;
					stmp.DeliveryMethod = SmtpDeliveryMethod.Network;
					stmp.Send(email);
				}
				return true;
			}
			catch (SmtpException)
			{
				return false;
			}
		}

		public static string FilterSQLChar(string str)
		{
			if (str == null || str == "")
			{
				return null;
			}

			string sInput1 = str.ToLower();
			string output = str;
			string pattern = @"*|and|exec|insert|select|delete|update|count|master|truncate|declare|char(|mid(|chr(|'";
			if (Regex.Match(sInput1, Regex.Escape(pattern), RegexOptions.Compiled | RegexOptions.IgnoreCase).Success)
			{
			}
			else
			{
				output = output.Replace("'", "''");
			}
			return output;
		}
	}
}
