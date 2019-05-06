# LUIS_QnA_LaLiga
Welcome to the La Liga Bot Demo!

To run the transcript directly, download Bot Framework Emulator and open the Transcript.transcript file in the Emulator. 

To run the demo locally, follow the steps below.

## Prerequisites

#### Setting up QnA Maker
- Follow instructions [here](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/how-to/set-up-qnamaker-service-azure)
to create a QnA Maker service.
- Follow instructions [here](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/quickstarts/create-publish-knowledge-base) to
import and publish the [LaligaQnAMaker.tsv](https://qnamakerppestorage.blob.core.windows.net/laliga-resource/LaligaQnAMaker.tsv) to your newly created QnA Maker service.
- Update [LaLiga.bot](./Laliga.bot) with your kbid (KnowledgeBase Id) and endpointKey in the "qna" services section. You can find this
information under "Settings" tab for your QnA Maker Knowledge Base at [QnAMaker.ai](https://www.qnamaker.ai)
- (Optional) Follow instructions [here](https://github.com/Microsoft/botbuilder-tools/tree/master/packages/QnAMaker) to set up the
QnA Maker CLI to deploy the model.

#### Setting up LUIS
- Navigate to [LUIS Portal](http://luis.ai)
- Click the `Sign in` button
- Click on `My apps` button
- Click on `Import new app`
- Click on the `Choose File` and select [LaLiagaLUIS.json](https://qnamakerppestorage.blob.core.windows.net/laliga-resource/LaligaLUIS.json).
- Update [LaLiga.bot](LaLiga.bot) file with your AppId, SubscriptionKey, Region and Version.
    You can find this information under "Manage" tab for your LUIS application at [LUIS portal](https://www.luis.ai).
    - The `AppID` can be found in "Application Information"
    - The `SubscriptionKey` can be found in "Keys and Endpoints", under the `Key 1` column
    - The `region` can be found in "Keys and Endpoints", under the `Region` column
- In [Luis Service](./Helpers/LuisService.cs), update SubscriptionKey(line: 28) and AppID(line: 29).    

## Run in Visual Studio
- Open the .sln file with Visual Studio.
- Press F5.
## Run in Visual Studio Code
- Open the bot project folder with Visual Studio Code.
- Bring up a terminal.
- Type 'dotnet run'.
## Testing the bot using Bot Framework Emulator
[Microsoft Bot Framework Emulator](https://aka.ms/botframework-emulator) is a desktop application that allows bot developers to test and debug
their bots on localhost or running remotely through a tunnel.
- Install the Bot Framework Emulator from [here](https://aka.ms/botframework-emulator).
### Connect to bot using Bot Framework Emulator
- Launch the Bot Framework Emulator
- File -> Open bot and navigate to the bot project folder
- Select `LaLiga.bot` file

# Basic Bot template
This bot has been created using [Microsoft Bot Framework](https://dev.botframework.com),
- Use [LUIS](https://luis.ai) to implement core AI capabilities
- Implement a multi-turn conversation using Dialogs
- Handle user interruptions for such things as Help or Cancel
- Prompt for and validate requests for information from the user

# Further reading
- [Bot Framework Documentation](https://docs.botframework.com)
- [Bot basics](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-basics?view=azure-bot-service-4.0)
- [Activity processing](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-concept-activity-processing?view=azure-bot-service-4.0)
- [LUIS](https://luis.ai)
- [Prompt Types](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-prompts?view=azure-bot-service-4.0&tabs=javascript)
- [Azure Bot Service Introduction](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-overview-introduction?view=azure-bot-service-4.0)
- [Channels and Bot Connector Service](https://docs.microsoft.com/en-us/azure/bot-service/bot-concepts?view=azure-bot-service-4.0)
- [QnA Maker](https://qnamaker.ai)

