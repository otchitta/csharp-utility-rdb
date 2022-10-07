using System;
using System.Collections.Generic;
using System.Data;
using Otchitta.Libraries.Common;
using Otchitta.Libraries.Common.Rdb;

namespace Otchitta.Utilities.Rdb.Compare;

/// <summary>
/// 情報比較処理クラスです。
/// </summary>
internal static class Program {
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

	#region 内部メソッド定義(抽出関連:SelectText)
	/// <summary>
	/// 抽出処理を実行します。
	/// </summary>
	/// <param name="command">実行処理</param>
	/// <returns>読込処理</returns>
	private static IDataReader SelectText(IDbCommand command) {
		try {
			return command.ExecuteReader();
		} catch {
			Logger.Error("[情報]抽出処理に失敗しました");
			Logger.Error("[情報]抽出構文:{0}", command.CommandText);
			var source = command.Parameters;
			for (var index = 0; index < source.Count; index ++) {
				var choose = source[index];
				if (choose is IDbDataParameter cache1) {
					Logger.Error("[情報]引数({0,2}):{1}={2}", index, cache1.ParameterName, cache1.Value);
				} else {
					Logger.Error("[情報]引数({0,2}):{1}", index, choose);
				}
			}
			throw;
		}
	}
	#endregion 内部メソッド定義(抽出関連:SelectText)

	#region 内部メソッド定義(比較関連:InvokeData/InvokeList)
	/// <summary>
	/// 引数情報を比較します。
	/// </summary>
	/// <param name="logging">ログ情報</param>
	/// <param name="record1">読込情報</param>
	/// <param name="record2">読込情報</param>
	/// <returns>全てが一致した場合、<c>True</c>を返却</returns>
	private static bool InvokeData(string logging, IDataRecord record1, IDataRecord record2) {
		var result = true;
		for (var index = 0; index < record1.FieldCount; index ++) {
			var value1 = record1[index];
			var value2 = record2[index];
			if (Equals(record1[index], record2[index])) {
			} else if (record1.GetName(index) == record2.GetName(index)) {
				Logger.Trace("[情報]値不一致:{0}.{1}({2}≠{3})", logging, record1.GetName(index), value1, value2);
				result = false;
			} else {
				Logger.Trace("[情報]値不一致:{0}.{1}:{2}({3}≠{4})", logging, record1.GetName(index), record2.GetName(index), value1, value2);
				result = false;
			}
		}
		return result;
	}
	/// <summary>
	/// 転送処理を実行します。
	/// </summary>
	/// <param name="settings">設定情報</param>
	/// <param name="command1">実行処理</param>
	/// <param name="command2">実行処理</param>
	/// <returns>比較結果(0:完全一致 1:値不一致 2:行不一致 3:列不一致)</returns>
	private static int InvokeList(Command settings, IDbCommand command1, IDbCommand command2) {
		Logger.Trace("[情報]比較名称:{0}", settings.SettingName);
		command1.CommandText = settings.SelectText1;
		command2.CommandText = settings.SelectText2;
		using (var reader1 = SelectText(command1))
		using (var reader2 = SelectText(command2)) {
			// 列数判定
			var count1 = reader1.FieldCount;
			var count2 = reader2.FieldCount;
			if (count1 != count2) {
				// 列数が同一ではない場合
				Logger.Trace("[情報]列不一致:{0}({1}≠{2})", settings.SettingName, count1, count2);
				return 3;
			} else {
				// 列数が同一である場合
				var count3 = 0;
				var count4 = 0;
				var result = true;
				while (reader1.Read()) {
					count3 ++;
					if (reader2.Read() == false) {
						// 行不一致
						while (reader1.Read()) count3 ++;
						Logger.Trace("[情報]行不一致:{0}({1}≠{2})", settings.SettingName, count3, count4);
						return 2;
					} else if (InvokeData(settings.SettingName, reader1, reader2) == false) {
						// 値不一致
						result = false;
					}
					count4 ++;
				}
				if (reader2.Read()) {
					count4 ++;
					// 行不一致
					while (reader2.Read()) count4 ++;
					Logger.Trace("[情報]行不一致:{0}({1}≠{2})", settings.SettingName, count3, count4);
					return 2;
				}
				if (result) {
					// 完全一致
					return 0;
				} else {
					// 値不一致
					return 1;
				}
			}
		}
	}
	/// <summary>
	/// 転送処理を実行します。
	/// </summary>
	/// <param name="settings">設定一覧</param>
	/// <param name="command1">実行処理</param>
	/// <param name="command2">実行処理</param>
	private static void InvokeList(IEnumerable<Command> settings, IDbCommand command1, IDbCommand command2) {
		foreach (var setting in settings) {
			switch (InvokeList(setting, command1, command2)) {
			case 0: // 完全一致
				break;
			case 1: // 値不一致
				Logger.Info("[情報]値不一致:{0}", setting.SettingName);
				break;
			case 2: // 行不一致
				Logger.Info("[情報]行不一致:{0}", setting.SettingName);
				break;
			case 3: // 列不一致
				Logger.Info("[情報]列不一致:{0}", setting.SettingName);
				break;
			}
		}
	}
	#endregion 内部メソッド定義(比較関連:InvokeData/InvokeList)

	#region 内部メソッド定義(実行関連:InvokeRoot)
	/// <summary>
	/// 転送処理を実行します。
	/// </summary>
	/// <param name="settings">設定情報</param>
	private static void InvokeRoot(Setting settings) {
		const string Connector = "System.Data.SqlClient";
		System.Data.Common.DbProviderFactories.RegisterFactory(Connector, System.Data.SqlClient.SqlClientFactory.Instance);
		using (var connection1 = ConnectUtilities.Create(Connector, settings.Parameter1))
		using (var connection2 = ConnectUtilities.Create(Connector, settings.Parameter2))
		using (var command1 = connection1.CreateCommand())
		using (var command2 = connection2.CreateCommand()) {
			Logger.Info("[情報]比較件数:{0}", settings.SelectList.Count);
			InvokeList(settings.SelectList, command1, command2);
		}
	}
	/// <summary>
	/// 転送処理を実行します。
	/// </summary>
	/// <param name="configFile">設定場所</param>
	private static void InvokeRoot(string configFile) {
		Logger.Info("[開始]=====================================================================");
		Logger.Info("[開始]データ転送処理");
		Logger.Info("[開始]---------------------------------------------------------------------");
		Logger.Info("[情報]設定ファイル:{0}", configFile);
		try {
			InvokeRoot(new Setting(configFile));
		} catch (Exception error) {
			Logger.Error(error, "[情報]想定外のエラーが発生しました");
		} finally {
			Logger.Info("[終了]=====================================================================");
		}
	}
	/// <summary>
	/// 転送処理を実行します。
	/// </summary>
	private static void InvokeRoot() =>
		InvokeRoot(ExeFileUtilities.GetAbsolutePath(ExeFileUtilities.GetExecuteName(true) + ".default.config"));
	#endregion 内部メソッド定義(実行関連:InvokeRoot)

	#region 起動メソッド定義
	/// <summary>
	/// 転送処理を実行します。
	/// </summary>
	/// <param name="commands">コマンドライン引数</param>
	public static void Main(string[] commands) {
		switch (commands.Length) {
		case 0: // 設定ファイル指定なしの場合
			InvokeRoot();
			break;
		case 1: // 設定ファイル指定ありの場合
			InvokeRoot(commands[0]);
			break;
		default: // 上記以外の場合
			Console.WriteLine("引数の指定が正しくありません。");
			Console.WriteLine("以下の形式で実行してください。");
			Console.WriteLine("> dotnet run [設定ファイル]");
			break;
		}
	}
	#endregion 起動メソッド定義
}
