# Lobotomy Corporation Mods

## About

There are Lobotomy Corporation's Mods.

これはロボトミーコーポレーションのModsです。

## Install

Comming Soon...

## List of Mods

----
### Weapon Speed Fix

Fix all weapons (23 types) that have unique animations and do not change attack speed with justice so that the speed changes.
(Pistol-type weapons also do not change, but these are not supported.)

Weapons added by other mods are basically unaffected.
(This is because they may have been adjusted based on the assumption of fixed speed.)

独自アニメーションを持つ、正義で攻撃速度が変化しない武器全て(23種)を速度が変化するように修正します。
(ピストル系も変化しませんが、こちらは非対応です。)

他のModによって追加される武器には基本的に影響しません。
(速度固定を前提に調整されている可能性があるため。)


----
### Inherit Agent

When you start on day 1, the previous agent is taken over.

What is taken over can be change in config.xml.
For example, only 80% of the status can be taken over, or only the gift can be taken over.

1日目から始めたときに前回のエージェントを引き継ぎます。

引き継ぐ内容はconfig.xmlで変更可能です。
例えば、ステータスを8割だけ引き継ぐことや、ギフトのみ引き継ぐことが出来ます。


----
### Config Status Max

Allows modification of various parameters related to status.
By default, the status limit is increased after the game is cleared.

Values can be set in config.xml.
Configurable parameters:
- Status limit
- Required status by Stat Level
- Growth rate by Stat Level
- Limit of Stat Levels that can be enhanced with Lob points
- Lob points required for enhancement and status range after enhancement

Note: Conflicts with E.G.O Gift Ceiling Mod (folder name: `BlacklightsC_GiftChanceBoost_MOD`) by BlacklightsC.
Use my Gift Chance Boost instead.


ステータスに関する様々なパラメータを変更可能にします。
デフォルトではゲームクリア後にステータス上限が増加するようになっています。

値はconfig.xmlで設定可能です。
設定可能なパラメータ:
- ステータスの上限
- 能力ランク別の必要ステータス
- 能力ランク別の成長率
- Lobポイントで強化できる能力ランクの上限
- 強化に必要なLobポイントと、強化後のステータス範囲

注意: BlacklightsC の E.G.O Gift Ceiling Mod (フォルダ名: `BlacklightsC_GiftChanceBoost_MOD`)と衝突します。
代わりに自分のGift Chance Boostを使用してください。


----
### Gift Chance Boost

When E.G.O gift is not obtained, the rate of obtaining it will be increased according to the result of the work. (Default: +100%, +50%, +25%)
The probability is managed per gift and is reset at the end of the day when the gift is obtained or at the end of the day.

The probability can be changed in config.xml.

The specifications are almost the same as [BlacklightsC's E.G.O Gift Ceiling Mod](https://gall.dcinside.com/mgallery/board/view/?id=lobotomycorporation&no=177028),
but the probability is managed per gift instead of abnormality.
It is also designed to reduce the possibility of conflicts with other mods.


E.G.O giftを入手できなかったとき、作業の結果によって入手率が上昇するようになります。(デフォルト:+100%, +50%, +25%)
確率はギフト毎に管理され、ギフトを入手するか1日の終わりにリセットされます。

確率はconfig.xmlで変更可能です。

[BlacklightsC の E.G.O Gift Ceiling Mod](https://gall.dcinside.com/mgallery/board/view/?id=lobotomycorporation&no=177028)
と仕様はほぼ同じですが、アブノーマリティではなくギフト別に確率が管理されます。
また、他のModと衝突する可能性を抑えた作りになっています。


----
### (Log Enable)

Logging tool for mod development.

Mod開発用ロギングツール。


----
## Development Environment

Visual Studio 2017


## Licence

**Note:** Since this is a derivative work, it must also follow [official guidelines](https://twitter.com/ProjMoonStudio/status/1629085462236397573).

The license for this itself is CC0.

これは二次創作物であるため、公式の[ガイドライン](https://twitter.com/ProjMoonStudio/status/1629085367491239936)
にも従わなければなりません。

これ自体のライセンスはCC0です。
