# KlingonWin10WPF
 From Scratch reimplementation of the Star Trek: Klingon Immersion Studies in a Windows 10 Compatible executable.  
 
 The latest version of Windows that you can play the original Star Trek: Klingon immersion studies is windows 98.
 
 The goal is to allow you to play the game on newer computers.
 
 You still need the original media to play.  This will not work without the content from the original disks.   No, I will not provide a copy of the disks or the disk content..  
 but you can get them on Ebay for pretty cheap now if you don't already have them.
 
 This remake of the game player is almost feature complete.  Three things to watch out for;
 1. In a certain scene where you have to press buttons in order, the buttons don't light up like they do in the original game yet.
 2. When you Quit, it doesn't ask you if you want to save yet, so Make sure to save before quitting!
 3. The window allows you to resize it.  While, this works 'mostly' OK, sometimes the click hotspots will be moved from where they should be.  Try to keep the black bars on the sides or top of the video to a minimum when you're resizing the window and you'll be fine.

---
 
## Build
This was built using Visual Studio 2019 Community Edition.  (freely downloadable)
Open the 'sln' file.   Make sure the nuget packages get restored.  Pick your platform.  Build.

The program will be built and put in the bin/x64/Debug folder.  If you build it for x86, it will be in the bin/x86/Debug folder.

---

## Media Setup
Once you have built this...  
This game doesn't work without some of the original media from the disks.
Inside the built folder, there is a folder called CDAssets.

From the original disks, you need to copy some of the AVI files to the CDAssets Folder.

From Disk 1:
- COMPUTER.AVI
- HOLODECK.AVI
- IP_1.AVI 
- MAIN_1.AVI
- SS_1.AVI 

From Disk 2: 
- MAIN_2.AVI 
- SS_2.AVI 

From the KlingonWin10WPF\CDAssets Folder:
- ffmpeg folder 
- prepareVideos.bat 

---
Once you have all of those files in the CDAssets folder.  
Run prepareVideos.bat.   
It should make X versions of 4 of the videos MAIN_1X.AVI, SS_1X.AVI, MAIN_2X.AVI, and SS_2X.AVI.  
The X versions have the audio re-synchronized.

At that point you should be able to run the game and have fun.

## Playing The Game

Just like the original game, it doesn't give you much information about how to play it.  

- Double click the video or press spacebar to pause the Holodeck program.  Double click or press space bar again to continue the Holodeck program.
- When you're in holodeck mode, you can click some things and the computer will tell you about them.
- Control the volume with the + and - buttons on the keyboard.
- Press 'S' on the keyboard to Save
- Press 'Q' on the keyboard to Quit (It won't ask you to save so watch out!)


## Debugging The Game

There is a debug overlay that you can load by pressing the Accent/Tilde button(`) on a US keyboard which gives you information about what hotspots are available and where your last click was in a mini square in the top left of the video and allows you to easily navigate around the game easily.
With the debug overlay open, and the video the last thing that you clicked on, 
Press 'C' to jump to the next challenge.
Press 'M' to jump 15 seconds forward in the video.
Press 'N' to jump 15 seconds back in the video.

## Legal Notice

The original game:  Star Trek:(TM) Klingon(tm) is published by Simon & Schuster Interactive, 
a devision of Simon & Schuster, 
in the publishing operation of Viacom Inc. 
1230 Avenue of the Americas, New York, NY 10020

Star Trek(TM) & (C) 1996 Paramount Pictures.  
All Rights Reserved. 

STAR TREK and Related Properties are Trademarks of Paramount Pictures. 
(c) 1996 Simon & Schuster Interactive, a devision of Simon & Schuster, Inc.

TrueMotion(R) is a registered trademark of The Duck Corporation.

Windows is a trademark and Microsoft is a registered trademark of Microsoft Corporation.

---

The remake of the FMV engine is completely from scratch and makes no use of the game assets or content.  Simon & Schuster never released code for the original edition. 

---

The code for the engine is released under the the MIT license.  It makes use of libVLC which is released under the GNU Lesser GPL licence, version 2.1.

If you need to contact me about a legal issue regarding this software, please do so at Teravus at gmail dot com.  Or Discord: RebootTech#6247

I also tend to stream on Twitch on Thursdays at around 4:30 PM Pacific time under the sceen name 'RebootTech'.

Binary release is coming soon.

