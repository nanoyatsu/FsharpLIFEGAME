// 参考：F#でWPFやるときのTipsとか(その1) - ぐるぐる～
// http://bleis-tift.hatenablog.com/entry/fsharp-wpf
// 参考：F#で謎カウンターを作るとき、let mutableでなくrefを使う（使っていた）のはなぜ？
// http://komorebikoboshi.hatenablog.com/entry/2014/12/19/100859

module App

open System
open FsXaml
open System.Windows

type MainView = XAML<"MainWindow.xaml">

[<STAThread>]
[<EntryPoint>]
let main argv =
    // XAML Type Provider によって生成された型のインスタンスを生成する
    let view = MainView()

    // デフォルト値つくる
    // OPTIMIZE:外部で持つべき（JSONにでも移し替える？）（これを？）
    let template_blinker = "□□□□□\n\
    □□■□□\n\
    □□■□□\n\
    □□■□□\n\
    □□□□□"

    let template_clock = "□□□□□□\n\
    □□□■□□\n\
    □■□■□□\n\
    □□■□■□\n\
    □□■□□□\n\
    □□□□□□"

    let template_beehive = "□□□□□\n\
    □□■□□\n\
    □■□■□\n\
    □■□■□\n\
    □□■□□\n\
    □□□□□"

    let template_pentadecathlon ="□□□□□□□□□□□□□□□□□□\n\
    □□□□□□□□□□□□□□□□□□\n\
    □□□□□□□□□□□□□□□□□□\n\
    □□□□□□□□□□□□□□□□□□\n\
    □□□□□□■□□□□■□□□□□□\n\
    □□□□□■■□□□□■■□□□□□\n\
    □□□□□□■□□□□■□□□□□□\n\
    □□□□□□□□□□□□□□□□□□\n\
    □□□□□□□□□□□□□□□□□□\n\
    □□□□□□□□□□□□□□□□□□\n\
    □□□□□□□□□□□□□□□□□□"

    let template_Array = 
        [|template_blinker;
          template_clock;
          template_beehive;
          template_pentadecathlon;
          |]
    let templateIndex = ref 0
    
    // 配列index循環用
    let loopAdder max n =
        if (n < max) 
        then n+1
        else 0    

    // 初期状態書いておく
    view.textBoxText.Text <- template_Array.[!templateIndex]

    printf "%A" view.textBoxText.Text
    // 文字列から処理用のシーケンスを作る
    // OPTIMIZE:textBoxから拾う処理だと遅延評価の意味がないのでは・・・？ できてから考える
    let createCelSeqFromString (inputStr:String) = 
        seq{ for s in inputStr.Split('\n') do
             yield seq{ for c in s do
                        if c = '■' then yield true else yield false
             }
        }
    
    // 演算部分 ↓ルール
    // 誕生
    // 死んでいるセルに隣接する生きたセルがちょうど3つあれば、次の世代が誕生する。
    // 生存
    // 生きているセルに隣接する生きたセルが2つか3つならば、次の世代でも生存する。
    // 過疎
    // 生きているセルに隣接する生きたセルが1つ以下ならば、過疎により死滅する。
    // 過密
    // 生きているセルに隣接する生きたセルが4つ以上ならば、過密により死滅する。
    let processPhase (nowSeq:seq<seq<bool>>) =
        let createNextMap _nowArray x y =
            let roundState (_nowArray:bool[][]) = 
                    if (0 < x && x < _nowArray.[0].Length-1) && (0 < y && y < _nowArray.Length-1) 
                    // HACK: 隣接セルの状態を拾うために苦肉の策の配列化 解決法が欲しい
                    then
                        [
                        _nowArray.[y-1].[x-1];_nowArray.[y-1].[x];_nowArray.[y-1].[x+1];
                        _nowArray.[y  ].[x-1];                    _nowArray.[y  ].[x+1];    
                        _nowArray.[y+1].[x-1];_nowArray.[y+1].[x];_nowArray.[y+1].[x+1]
                        ]
                    else
                            [false]
            let checkAlive isAlive = function
                | 2 -> isAlive
                | 3 -> true
                | _ -> false

            roundState _nowArray
            |> List.sumBy (fun x -> if x then 1 else 0)
            |> int
            |> checkAlive _nowArray.[y].[x]

        let nowArray = 
            seq{for y in nowSeq do
                yield Seq.toArray y
                } |> Seq.toArray

        seq{for y in 0..nowArray.Length-1 do
            yield seq{for x in 0..nowArray.[y].Length-1 do
                        yield createNextMap nowArray x y
                    }
            }

    let printPhase nextSeq =
        let printCell nextCel =
            view.textBoxText.AppendText (if nextCel then "■" else "□")
        let printLine nextLine =
            Seq.iter printCell nextLine |> ignore
            view.textBoxText.AppendText "\n"

        view.textBoxText.Clear()
        nextSeq
        |> Seq.iter printLine |> ignore
        view.textBoxText.Text <- view.textBoxText.Text.Trim('\n')
    
    // イベント関数 実質操作ここだけ
    view.btnPlusOne.Click.Add(fun _ -> 
        view.textBoxText.Text
        |> createCelSeqFromString
        |> processPhase
        |> printPhase )

    // イベント関数 template入れ替え
    view.btnAnotherTemplate.Click.Add(fun _ ->
        templateIndex := loopAdder (template_Array.Length-1) !templateIndex
        view.textBoxText.Text <- template_Array.[!templateIndex]
    )
    
    // 画面つくり
    // Application.Run にXAMLのRoot要素であるWindowオブジェクトを渡して、
    // アプリケーションを起動
    let app = Application()
    app.Run(view)