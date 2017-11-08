### Boostnote: https://boostnote.io/
### Trello: https://trello.com/

The purpose of this program is to convert a Boostnote Markdown note into a Trello card

## Setup:

1. Download the most recent release (https://github.com/derekantrican/BoostnoteToTrello/releases)
2. Run BoostnoteToTrello.exe (it will immediately quit, but it will open a authorization window for Trello - **SAVE YOUR USER TOKEN AFTER AUTHORIZING**)
3. Start command prompt and change the directory to the loaction of BoostnoteToTrello.exe
4. Use the following command: `BoostnoteToTrello.exe -userToken "THE_USER_TOKEN_FROM_STEP_2" -file "THE_LOCATION_OF_THE_NOTE" -cardId "THE_ID_OF_THE_TRELLO_CARD"` (or, if you just want to see the parameters needed, run the command `BoostnoteToTrello.exe -help`)

#### A couple notes:

- The location of the Boostnote note is likely something similar to: `%userprofile%\Boostnote\notes\0a5de9a4c171eb481b05.cson` *(note that every note will have its own unique id as its name and it can change sometimes too. Open the note in notepad or something similar to make sure you have the right one)*
- You can find your Trello card id from the URL of the card. For instance, `https://trello.com/c/AzCNJbTG/393-welcome-to-boostnote` would mean your card id is `AzCNJbTG`


------

### Maintenance

I'll handle some small issues for a bit, but this was mostly a proof-of-concept project, so I don't expect to put much effort into maintaining it

------

### Used libraries:

- Costura.Fody: https://github.com/Fody/Costura
- Manatee.Trello: https://github.com/gregsdennis/Manatee.Trello
