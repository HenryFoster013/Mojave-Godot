### Project Mojave ###
## Introduction ##
This is an online emulation of my custom Fallout themed RISK board, for use on the web. I want this to be able to embed into a webpage so anyone can play games on the board digitally with friends.

This is an ongoing passion project, a small strategy game based on RISK with a few changes. It's set on a single board and (will) support both local bot-play and multiplayer. Each playable colour is indicitive of a faction within the series, though there is no gameplay differences between them. Roleplay between players is encouraged.

The board itself is based on the design by AMCAImaron on DeviantArt
[Original post here](https://www.deviantart.com/amcalmaron/art/RISK-Mojave-Wasteland-Draft-723058736)

## Technical
The project was originally written for Unity version 2021.3.45f2 as it was my preferred LTS release before the changes to the input system. I also planned to use PUN2 for networking. I've made the sudden switch to Godot and I'm unsure of my networking solution as of yet.
[Original Unity repro here](https://github.com/HenryFoster013/Mojave)


## A Rant on Unity ##
I already programmed the majority of this on Unity. My usual game engine. So Henry, why the sudden switch to Godot?

Simply put, I am sick of Unity. I hate their business decisions and their software is so bloated and hard to manage I'm having to find loopholes around the engines renderer just to barely get the effects I need. Why bother!

I program on a Thinkpad T430. From 2011, with the original hardware and battery. I got this laptop for free dumpster diving about 5 years ago and I don't intend to give up on her now. Unity really struggles on this thing, some of my more optimised projects like Kimber run great at a full 60fps but working on Mojave has been a real slug fest. It doesn't help the workarounds I've been using are adding heaps of lag.

More importantly, the older unity 202X versions have broken on the newest version of Xubuntu, which leaves me stranded. My laptop isn't powerful enough to run Unity 6.

So I decided to re-write the game in generic C# classes. I've tinkered with other ways of displaying the interface and even tried writing my own in .NET but for web integration I've chosen Godot which doesn't absolutely wreck my machine. It also offers the customisation I've been looking for. So I've decided to move development here. It also gives me a rare chance to rewrite a lot of my older code better, particularily the way I've been handling data management.

Hopefully this progresses further than last time.

## The (physical) Board ##

This christmas break, I had some spare time so I built a homemade RISK board based on the Fallout series. Me and a few of my (nerdier) friends all love RISK, the sessions were great even if they did get a little larpy. It was a lot of fun and it seems a shame to let the sessions end now that we've moved back out for University, so I started this as an effort to digitise the board; so we can still play the odd game despite the distance.

My physical board, currently hanging on my wall: 
<img width="1000" height="1000" alt="image" src="https://github.com/user-attachments/assets/0a3a21da-6198-4c05-ba3b-48fdd32005a0" />

I love strategy games and I've always wanted to make a digital verison of RISK, I've attempted a few times and never felt I had the technical know-how to complete it. After completing my previous project Colonial, and doing some research into how games such a CK3 work; I've felt pretty inspired to give it another shot. This is just a casual passion project, I don't have any big plans for releases or a schedule or anything so just bare with me.
