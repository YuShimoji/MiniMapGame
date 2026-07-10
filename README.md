# MiniMapGame

Unity 6.3 LTS / URP で構築している、プロシージャルなマップ生成と探索表現の
プロジェクトです。最終製品の定義はまだ固定せず、現在の active lane と判断事項を
リポジトリ内の正本で管理しています。

## 現在地を確認する

- [人が読む現在地](docs/project-context.md)
- [次セッション用の継続状態](docs/runtime-state.md)
- [仕様一覧と進捗](docs/spec-index.json)
- [AI 作業ルール](docs/ai/AGENT_RULES.md)

README には変動する進捗を複製しません。上記の canonical documents が更新される
ことで、監修AI、開発AI、人間の参照先が同じ状態を指す設計です。

## 開発を始める

要求される Unity Editor は `ProjectSettings/ProjectVersion.txt` に固定されています。
ローカルの一括 preflight は PowerShell から実行できます。

```powershell
.\tools\validate-project.ps1
```

Unity の import / script compile、ドキュメント、browser preview の構文、canonical
state の整合、Unity Asset metadata をまとめて確認します。Editor 上の操作と手動確認は
[Debug Setup & Verification Guide](docs/debug-setup.md) を参照してください。

## ドキュメントをブラウザで見る

```powershell
python -m pip install mkdocs-material
python -m mkdocs serve
```

`http://127.0.0.1:8000/` に、同じ `docs/` を元にした閲覧用 Project Hub が開きます。
この表示は派生ビューであり、別の status 正本ではありません。
