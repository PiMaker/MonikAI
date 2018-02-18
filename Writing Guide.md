Right now the application is **very** limited, it only responds at startup, to reddit.com and twitter.com. Also at the opening of either Firefox or Chrome (I don't think I can add more browsers anyway, these are the only two where I can read the currently open website URL).

For now, it can react to two events:

- Program launches: Anytime a .exe file is started (aka. you opening an application or a game)
- Web sites being opened: Opening of a website in Firefox or Chrome. It has a timeout, so for one specific web page, a message can only appear every 10 minutes at maximum. That is, if the URL changes, but the new page has been opened in the previous 10 minutes no text will be displayed - to avoid spamming. For now, it only works per web page (so no different reaction for specific twitter pages for example), however I'm going to change that soon, so if you want you can assume that's how it works.

A "message" being shown consists of a list of responses: Every time an action is triggered, one of the responses will be chosen at random (to keep her from saying the same thing over and over again, so for example you could have multiple dialogs for one web page and it will choose one at random every time that page is opened). A "response" (the thing that is chosen to be displayed by Monika) contains one or more lines of text. One line can be about 90 characters long, give or take a few depending on where it has to break and character size (e.g. W is bigger than i). For each line of text, a face can be chosen. For a list of faces check [this folder](https://github.com/PiMaker/MonikAI/tree/master/MonikAI/monika) (Tip: If you go to releases and download the "Source Code.zip" you can find all images in there for easier looking).

An example of a web page response would be as follows:
`
reddit.com:
  Response 1:
    Reddit is cool! (Face: k)
  Response 2:
    Reddit is amazing! (Face: b)
    It really is! (Face: c)
`

So anytime reddit.com is opened, it would either display "Reddit is cool!" with face k OR display "Reddit is amazing!" followed immediately by "It really is!" with the according faces.

Thank you for being interested in helping!
