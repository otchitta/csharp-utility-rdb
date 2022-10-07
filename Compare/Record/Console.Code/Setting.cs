using System.Collections.ObjectModel;
using System.Xml;
using Otchitta.Libraries.Common.Xml;

namespace Otchitta.Utilities.Rdb.Compare;

/// <summary>
/// 設定情報クラスです。
/// </summary>
internal sealed class Setting {
	#region ログ出力処理定義
	/// <summary>
	/// ログ出力処理
	/// </summary>
	private static NLog.ILogger? logger;
	/// <summary>
	/// ログ出力処理を取得します。
	/// </summary>
	/// <returns>ログ出力処理</returns>
	private static NLog.ILogger Logger => logger ??= NLog.LogManager.GetCurrentClassLogger();
	#endregion ログ出力処理定義

	#region プロパティー定義
	/// <summary>
	/// 接続引数１を取得します。
	/// </summary>
	/// <value>接続引数１</value>
	public string Parameter1 {
		get;
	}
	/// <summary>
	/// 接続引数２を取得します。
	/// </summary>
	/// <value>接続引数２</value>
	public string Parameter2 {
		get;
	}
	/// <summary>
	/// 構文一覧を取得します。
	/// </summary>
	public ReadOnlyCollection<Command> SelectList {
		get;
	}
	#endregion プロパティー定義

	#region 生成メソッド定義
	/// <summary>
	/// 設定情報を生成します。
	/// </summary>
	/// <param name="settings">設定情報</param>
	public Setting(XmlNode settings) {
		Parameter1 = settings.GetString("parameter1");
		Parameter2 = settings.GetString("parameter2");
		SelectList = new ReadOnlyCollection<Command>(Command.CreateList(settings));
		Logger.Debug("[設定]接続引数１:{0}", Parameter1);
		Logger.Debug("[設定]接続引数２:{0}", Parameter2);
	}
	/// <summary>
	/// 設定情報を生成します。
	/// </summary>
	/// <param name="settings">設定情報</param>
	public Setting(string settings) : this(XmlNodeUtilities.Create(settings)) {
		// 処理なし
	}
	#endregion 生成メソッド定義
}
