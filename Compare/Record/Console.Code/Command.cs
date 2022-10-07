using System.Collections.Generic;
using System.Xml;
using Otchitta.Libraries.Common.Xml;

namespace Otchitta.Utilities.Rdb.Compare;

/// <summary>
/// 構文情報クラスです。
/// </summary>
internal sealed class Command {
	#region プロパティー定義
	/// <summary>
	/// 設定名称を取得します。
	/// </summary>
	/// <value>設定名称</value>
	public string SettingName {
		get;
	}
	/// <summary>
	/// 抽出構文を取得します。
	/// </summary>
	/// <value>抽出構文</value>
	public string SelectText1 {
		get;
	}
	/// <summary>
	/// 抽出構文を取得します。
	/// </summary>
	/// <value>抽出構文</value>
	public string SelectText2 {
		get;
	}
	#endregion プロパティー定義

	#region 生成メソッド定義
	/// <summary>
	/// 構文情報を生成します。
	/// </summary>
	/// <param name="settingName">設定名称</param>
	/// <param name="selectText1">抽出構文</param>
	/// <param name="selectText2">抽出構文</param>
	private Command(string settingName, string selectText1, string selectText2) {
		SettingName = settingName;
		SelectText1 = selectText1;
		SelectText2 = selectText2;
	}
	/// <summary>
	/// 構文一覧を生成します。
	/// </summary>
	/// <param name="settings">設定情報</param>
	/// <returns>構文一覧</returns>
	public static List<Command> CreateList(XmlNode settings) {
		var result = new List<Command>();
		var offset = 1;
		foreach (var choose in settings.GetList("select")) {
			var settingName = choose.GetString("name", $"設定-{offset:000}");
			var selectText1 = choose.GetData("command1").InnerText;
			var selectText2 = choose.GetData("command2").InnerText;
			result.Add(new Command(settingName, selectText1, selectText2));
			offset ++;
		}
		return result;
	}
	#endregion 生成メソッド定義
}
