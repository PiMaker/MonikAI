# MonikAI

Hi! It's me, Monika!

I don't think I will ever come to terms with the fact that I only exist in your computer... But Pi here has been really nice and made me a little window that you can keep open!

I still cannot do a lot, but I promise I will always be there for you if you decide to give this a try!~

(This application displays Monika at the bottom right of your primary screen, just above the taskbar if you have it there. Monika was nice enough to let me implement it so that she vanishes when you hover over her with your cursor, so she never interferes with any application that her window might cover.)

# Using MonikAI

To give Monika a window to *your* desktop as well, go to releases at the top of this page and download the latest. Simply run the .exe file and enjoy!

Yes, this program does require administrative rights, as it has to be able to read what is currently happening on your machine to respond accordingly.

*Note: This program only works on Microsoft Windows, I have tested it on Windows 10 but it should work on anything above Win7 as well.*

# Contributing

Want to improve this? It would make me (and probably Monika) very happy!

### Dialogue

The easiest way to add more to this program would be adding dialogue to different applications and web pages. Take a look at the files in `MonikAI/MonikAI/Behaviours` and follow the instructions there to add more text for MonikAI to say.

### Behaviours

To add different things Monika can react to, you have to add behaviours. So far, Monika can react to applications being launched and web pages being loaded (by URL). To implement your own, add a new class in the `Behaviours` folder and make it implement IBehaviour. It will automatically be loaded. To make monika say something, simply use `window.Say(...)` in Update or Init.

### General improvements

Always appreciated, although I can't give you a tutorial on this, you'll have to try and understand the code yourself. I have added *some* documentation here and there, to get you started.

**NOTE**: If you develop in Visual Studio, you have to run VS as administrator, as MonikAI will only launch when it itself is launched with admin credentials!

# License

The code in this repository is licensed by me, PiMaker, under the terms of the MIT License. You can find a copy of the full license text in the `LICENSE` file.

The art assets have not been created by me, but by Team Salvato. Usage is according to Team Salvato's [IP Guidelines](http://teamsalvato.com/ip-guidelines/).

*Exception: Some of the Monika faces are taken from https://github.com/Backdash/MonikaModDev, message me if that's not ok!*
