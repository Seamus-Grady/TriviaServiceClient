using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using System.Dynamic;
using System.Runtime.InteropServices;

namespace TriviaServiceClientAlpha
{
    class Program
    {
        private static System.Timers.Timer myTimer = new System.Timers.Timer();
        private static System.Timers.Timer myTimerActive = new System.Timers.Timer();

        private static string URL = "http://localhost:60000/";

        const int STD_INPUT_HANDLE = -10;

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CancelIoEx(IntPtr handle, IntPtr lpOverlapped);

        private static CancellationTokenSource tokenSource;
        private static Dictionary<int, BoardNode> board;
        private static int purpleStart;
        private static int orangeStart;
        private static int greenStart;
        private static int pinkStart;
        private static int blueStart;
        private static int yellowStart;
        private static Player currentPlayer;
        private static string realAnswer;
        private static string userToken;
        private static string gameID;
        private static string playerTurn;
        private static int numberofPlayers = 0;
        private static string userName;
        private static string currentPlayerUserName;
        private static Player player1;
        private static Player player2;
        private static Player player3;
        private static Player player4;
        private static Task endlessTask;
        static void Main(string[] args)
        {
            Console.Write("Hello Welcome to Trivia Pursuit please enter a username: ");
            RegisterUser(Console.ReadLine());
            endlessTask = new TaskCompletionSource<bool>().Task;
            endlessTask.Wait();
        }

        private static async void  RegisterUser(string nickname)
        {
            try
            {
                using (HttpClient client = CreateClient(URL))
                {
                    tokenSource = new CancellationTokenSource();
                    StringContent content = new StringContent(JsonConvert.SerializeObject(nickname), Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync("TriviaService/users", content, tokenSource.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        userToken = (string)JsonConvert.DeserializeObject(result);
                        userName = nickname;
                        DisplayOnlineMenu();

                    }
                    else
                    {
                        Console.WriteLine("Error registering: " + response.StatusCode + "\n" + response.ReasonPhrase);
                    }
                }
            }
            catch (TaskCanceledException) { }
            catch(UriFormatException exception)
            {
                Console.WriteLine(exception.Message);
            }
        }

        private static async void JoinGame(string userToken, string gameID)
        {
            try
            {
                using (HttpClient client = CreateClient(URL))
                {
                    dynamic user = new ExpandoObject();
                    user.UserToken = userToken;
                    user.gameID = gameID;
                    tokenSource = new CancellationTokenSource();
                    StringContent content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync("TriviaService/join-game", content, tokenSource.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        playerTurn = (string)JsonConvert.DeserializeObject(result);
                        myTimer.Elapsed += (sender, e) => DisplayLobby(sender, e, false);
                        myTimer.Interval = 1000;
                        Console.Clear();
                        myTimer.Enabled = true;
                        StartOrExitGame(false, userToken, gameID, playerTurn);

                    }
                    else
                    {
                        Console.WriteLine("Error registering: " + response.StatusCode + "\n" + response.ReasonPhrase);
                    }
                }
            }
            catch (TaskCanceledException) { }
            catch (UriFormatException exception)
            {
                Console.WriteLine(exception.Message);
            }
        }
        private static async void StartOrExitGame(bool isAHost, string userToken, string gameID, string playerTurn)
        {
            string userInput = "";
            while(true)
            {
                if(userInput.Equals("exit") || isAHost && userInput.Equals("start"))
                {
                    break;
                }
                userInput = Console.ReadLine().ToLower();
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.Write(new String(' ', Console.BufferWidth));
            }
            if(userInput.Equals("exit"))
            {
                try
                {
                    using (HttpClient client = CreateClient(URL))
                    {
                        tokenSource = new CancellationTokenSource();
                        dynamic user = new ExpandoObject();
                        user.UserToken = userToken;
                        user.gameID = gameID;
                        user.playerTurn = playerTurn;
                        StringContent content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                        HttpResponseMessage response = await client.PutAsync("TriviaService/games", content, tokenSource.Token);

                        if(!response.IsSuccessStatusCode)
                        {
                            switch (response.StatusCode) { }
                        }
                        else
                        {
                            myTimer.Enabled = false;
                            DisplayOnlineMenu();
                        }
                    }
                }
                catch (TaskCanceledException) { }
            }
            if (userInput.Equals("start"))
            {
                try
                {
                    using (HttpClient client = CreateClient(URL))
                    {
                        tokenSource = new CancellationTokenSource();
                        dynamic user = new ExpandoObject();
                        user.UserToken = userToken;
                        user.gameID = gameID;
                        StringContent content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                        HttpResponseMessage response = await client.PutAsync("TriviaService/start-game", content, tokenSource.Token);

                        if (!response.IsSuccessStatusCode)
                        {
                            switch (response.StatusCode) { }
                        }
                    }
                }
                catch (TaskCanceledException) { }
            }
        }

        private static async void CreateGame(string userToken)
        {
            try
            {
                using (HttpClient client = CreateClient(URL))
                {
                    dynamic user = new ExpandoObject();
                    user.UserToken = userToken;
                    tokenSource = new CancellationTokenSource();
                    StringContent content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync("TriviaService/create-game", content, tokenSource.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        gameID = (string)JsonConvert.DeserializeObject(result);
                        playerTurn = "0";
                        myTimer.Elapsed += (sender, e) => DisplayLobby(sender, e, true);
                        myTimer.Interval = 1000;
                        Console.Clear();
                        myTimer.Enabled = true;
                        StartOrExitGame(true, userToken, gameID, "0");

                    }
                    else
                    {
                        Console.WriteLine("Error registering: " + response.StatusCode + "\n" + response.ReasonPhrase);
                    }
                }
            }
            catch (TaskCanceledException) { }
            catch (UriFormatException exception)
            {
                Console.WriteLine(exception.Message);
            }
        }
        private static async void StartGame()
        {
            int color;
            int position;
            CreateBoard();
            currentPlayer = new Player();
            currentPlayer.userName = userName;
            Console.Clear();
            Console.WriteLine("What color would you like your piece?\n1.Pink\n2.Green\n3.Blue\n4.Orange" +
                "\n5.Yellow\n6.Purple");
            while (!int.TryParse(Console.ReadLine(), out color) || color < 1 || color > 6)
            {
                Console.WriteLine("Error incorrent input: Please enter a number 1-6");
                Console.WriteLine("What color would you like your piece?\n1.Pink\n2.Green\n3.Blue\n4.Orange" +
                "\n5.Yellow\n6.Purple");
            }
            Console.Clear();
            Console.WriteLine("What color position would you like to start at?\n1.Pink\n2.Green\n3.Blue\n4.Orange" +
                "\n5.Yellow\n6.Purple");
            while (!int.TryParse(Console.ReadLine(), out position) || position < 1 || position > 6)
            {
                Console.WriteLine("Error incorrent input: Please enter a number 1-6");
                Console.WriteLine("What color would you like your piece?\n1.Pink\n2.Green\n3.Blue\n4.Orange" +
                "\n5.Yellow\n6.Purple");
            }
            switch (position)
            {
                case 1:
                    currentPlayer.CurrentPosition = pinkStart;
                    break;
                case 2:
                    currentPlayer.CurrentPosition = greenStart;
                    break;
                case 3:
                    currentPlayer.CurrentPosition = blueStart;
                    break;
                case 4:
                    currentPlayer.CurrentPosition = orangeStart;
                    break;
                case 5:
                    currentPlayer.CurrentPosition = yellowStart;
                    break;
                case 6:
                    currentPlayer.CurrentPosition = purpleStart;
                    break;
            }
            try
            {
                using (HttpClient client = CreateClient(URL))
                {
                    tokenSource = new CancellationTokenSource();
                    dynamic user = new ExpandoObject();
                    user.UserToken = userToken;
                    user.gameID = gameID;
                    user.currentPosition = currentPlayer.CurrentPosition;
                    StringContent content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PutAsync("TriviaService/game-setup", content, tokenSource.Token);

                    if (!response.IsSuccessStatusCode)
                    {
                        switch (response.StatusCode) { }
                    }
                    else
                    {
                        myTimerActive.Elapsed += DisplayPlayersTurn;
                        myTimerActive.Interval = 1000;
                        myTimerActive.Enabled = true;
                    }
                }
            }
            catch (TaskCanceledException) { }
        }
        private static async void DisplayPlayersTurn(Object myObject, EventArgs myEventArgs)
        {
            try
            {
                using (HttpClient client = CreateClient(URL))
                {
                    string tickURL = string.Format("TriviaService/games/{0}/{1}", gameID, "false");
                    HttpResponseMessage response = await client.GetAsync(tickURL, tokenSource.Token);

                    if(response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        dynamic itemToken = JsonConvert.DeserializeObject(result);
                        var currentPlayerToken = itemToken.currentPlayer;
                        var player1 = itemToken.Player1;
                        var player2 = itemToken.Player2;
                        var player3 = itemToken.Player3;
                        var player4 = itemToken.Player4;
                        if (currentPlayerToken == null)
                        {
                            Console.Clear();
                            Console.WriteLine("Congratulations " + currentPlayerUserName + "You won!");
                        }
                        if(currentPlayerToken.Nickname.Equals(userName))
                        {
                            myTimerActive.Enabled = false;
                            currentPlayerUserName = currentPlayerToken.Nickname;
                            currentPlayer.Geography = currentPlayerToken.Geography;
                            currentPlayer.Entertainment = currentPlayerToken.Entertainment;
                            currentPlayer.History = currentPlayerToken.History;
                            currentPlayer.Art = currentPlayerToken.Art;
                            currentPlayer.Science = currentPlayerToken.Science;
                            currentPlayer.Sports = currentPlayerToken.Sports;
                            Console.SetCursorPosition(0, 0);
                            if (player1 != null)
                            {
                                Console.Write(new string(' ', Console.BufferWidth));
                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                Console.WriteLine("Player1:" + player1.NickName);
                                Console.Write(new string(' ', Console.BufferWidth));
                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                Console.WriteLine("Geography:" + player1.Geography + "Entertainment:" + player1.Entertainment + "History:" + player1.History + "Art:" + player1.Art + "Science:" + player1.Science + "Sports/Leisure:" + player1.Sports);
                            }
                            if (player2 != null)
                            {
                                Console.Write(new string(' ', Console.BufferWidth));
                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                Console.WriteLine("Player2:" + player2.NickName);
                                Console.Write(new string(' ', Console.BufferWidth));
                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                Console.WriteLine("Geography:" + player2.Geography + "Entertainment:" + player2.Entertainment + "History:" + player2.History + "Art:" + player2.Art + "Science:" + player2.Science + "Sports/Leisure:" + player2.Sports);
                            }
                            if (player3 != null)
                            {
                                Console.Write(new string(' ', Console.BufferWidth));
                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                Console.WriteLine("Player3:" + player3.NickName);
                                Console.Write(new string(' ', Console.BufferWidth));
                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                Console.WriteLine("Geography:" + player3.Geography + "Entertainment:" + player3.Entertainment + "History:" + player3.History + "Art:" + player3.Art + "Science:" + player3.Science + "Sports/Leisure:" + player3.Sports);
                            }
                            if (player4 != null)
                            {
                                Console.Write(new string(' ', Console.BufferWidth));
                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                Console.WriteLine("Player4:" + player4.NickName);
                                Console.Write(new string(' ', Console.BufferWidth));
                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                Console.WriteLine("Geography:" + player4.Geography + "Entertainment:" + player4.Entertainment + "History:" + player4.History + "Art:" + player4.Art + "Science:" + player4.Science + "Sports/Leisure:" + player4.Sports);
                            }
                            Console.Clear();
                            PlayerTurn();
                        }
                        if(currentPlayerUserName.Equals(currentPlayerToken.Nickname))
                        {
                            
                            Console.SetCursorPosition(0, 0);
                            if(player1 != null)
                            {
                                Console.Write(new string(' ', Console.BufferWidth));
                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                Console.WriteLine("Player1:" + player1.NickName);
                                Console.Write(new string(' ', Console.BufferWidth));
                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                Console.WriteLine("Geography:" + player1.Geography + "Entertainment:" + player1.Entertainment + "History:" + player1.History + "Art:" + player1.Art + "Science:" + player1.Science + "Sports/Leisure:" + player1.Sports);
                            }
                            if (player2 != null)
                            {
                                Console.Write(new string(' ', Console.BufferWidth));
                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                Console.WriteLine("Player2:" + player2.NickName);
                                Console.Write(new string(' ', Console.BufferWidth));
                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                Console.WriteLine("Geography:" + player2.Geography + "Entertainment:" + player2.Entertainment + "History:" + player2.History + "Art:" + player2.Art + "Science:" + player2.Science + "Sports/Leisure:" + player2.Sports);
                            }
                            if (player3 != null)
                            {
                                Console.Write(new string(' ', Console.BufferWidth));
                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                Console.WriteLine("Player3:" + player3.NickName);
                                Console.Write(new string(' ', Console.BufferWidth));
                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                Console.WriteLine("Geography:" + player3.Geography + "Entertainment:" + player3.Entertainment + "History:" + player3.History + "Art:" + player3.Art + "Science:" + player3.Science + "Sports/Leisure:" + player3.Sports);
                            }
                            if (player4 != null)
                            {
                                Console.Write(new string(' ', Console.BufferWidth));
                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                Console.WriteLine("Player4:" + player4.NickName);
                                Console.Write(new string(' ', Console.BufferWidth));
                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                Console.WriteLine("Geography:" + player4.Geography + "Entertainment:" + player4.Entertainment + "History:" + player4.History + "Art:" + player4.Art + "Science:" + player4.Science + "Sports/Leisure:" + player4.Sports);
                            }
                            Console.WriteLine("Current Player Turn " + currentPlayerUserName);
                            Console.Write(new string(' ', Console.BufferWidth));
                            Console.SetCursorPosition(0, Console.CursorTop - 1);
                            Console.WriteLine("Geography:" + currentPlayerToken.Geography + "Entertainment:" + currentPlayerToken.Entertainment + "History:" + currentPlayerToken.History + "Art:" + currentPlayerToken.Art + "Science:" + currentPlayerToken.Science + "Sports/Leisure:" + currentPlayerToken.Sports);
                            Console.Write(new string(' ', Console.BufferWidth));
                            if (itemToken.currentQuestion != null)
                            {
                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                Console.WriteLine("Current Question: " + itemToken.currentQuestion);
                            }
                            Console.Write(new string(' ', Console.BufferWidth));
                            if(itemToken.currentQuestionAnswer != null)
                            {
                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                Console.WriteLine("Current Question's Answer: " + itemToken.currentQuestionAnswer);
                            }

                        }
                        if(!currentPlayerUserName.Equals(currentPlayerToken.Nickname))
                        {
                            currentPlayerUserName = currentPlayerToken.Nickname;
                            Console.Clear();
                            Console.WriteLine("Player " + currentPlayerUserName + " Turn");
                        }
                    }
                }
            }
            catch (TaskCanceledException) { }
        }
        private static async void DisplayLobby(Object myObject, EventArgs myEventArgs, bool isHost)
        {
            try
            {
                using (HttpClient client = CreateClient(URL))
                {
                    tokenSource = new CancellationTokenSource();
                    string tickURL = string.Format("TriviaService/games/{0}/{1}", gameID, "true");
                    HttpResponseMessage response = await client.GetAsync(tickURL, tokenSource.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        dynamic itemToken = JsonConvert.DeserializeObject(result);
                        var player1 = itemToken.Player1;
                        var player2 = itemToken.Player2;
                        var player3 = itemToken.Player3;
                        var player4 = itemToken.Player4;
                        var curren = itemToken.currentPlayer;
                        if (curren != null)
                        {
                            myTimer.Enabled = false;
                            var handle = GetStdHandle(STD_INPUT_HANDLE);
                            CancelIoEx(handle, IntPtr.Zero);
                            if(player1 != null)
                            {
                                numberofPlayers++;
                            }
                            if (player2 != null)
                            {
                                numberofPlayers++;
                            }
                            if (player3 != null)
                            {
                                numberofPlayers++;
                            }
                            if (player4 != null)
                            {
                                numberofPlayers++;
                            }
                            StartGame();
                        }
                        else
                        {
                            int oldPosition = Console.CursorLeft;
                            Console.SetCursorPosition(0, 0);
                            Console.WriteLine("Lobby " + gameID);
                            if (player1 == null && player2 == null && player3 == null && player4 == null)
                            {
                                myTimer.Enabled = false;
                                DisplayOnlineMenu();
                            }
                            else
                            {
                                if (player1 != null && player3 != null)
                                {
                                    Console.Write(new String(' ', Console.BufferWidth));
                                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                                    Console.WriteLine("1. " + player1.Nickname + " 3. " + player3.Nickname);
                                }
                                if (player2 != null && player4 != null)
                                {
                                    Console.Write(new String(' ', Console.BufferWidth));
                                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                                    Console.WriteLine("2. " + player2.Nickname + " 4. " + player4.Nickname);
                                }
                                if (player1 != null && player3 == null)
                                {
                                    Console.Write(new String(' ', Console.BufferWidth));
                                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                                    Console.WriteLine("1. " + player1.Nickname + " 3.");
                                }
                                if (player2 != null && player4 == null)
                                {
                                    Console.Write(new String(' ', Console.BufferWidth));
                                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                                    Console.WriteLine("2. " + player2.Nickname + " 4. ");
                                }
                                if (player1 == null && player3 != null)
                                {
                                    Console.Write(new String(' ', Console.BufferWidth));
                                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                                    Console.WriteLine("1. " + " 3." + player3.Nickname);
                                }
                                if (player2 == null && player4 != null)
                                {
                                    Console.Write(new String(' ', Console.BufferWidth));
                                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                                    Console.WriteLine("2. " + " 4. " + player4.Nickname);
                                }
                                if (player1 == null && player3 == null)
                                {
                                    Console.Write(new String(' ', Console.BufferWidth));
                                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                                    Console.WriteLine("1. 3.");
                                }
                                if (player2 == null && player4 == null)
                                {
                                    Console.Write(new String(' ', Console.BufferWidth));
                                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                                    Console.WriteLine("2. 4.");
                                }
                                if (isHost)
                                {
                                    Console.WriteLine("If you would like to start the game enter start If you would like to exit enter exit");
                                }
                                else
                                {
                                    Console.WriteLine("If you would like to exit enter exit");
                                }
                                Console.SetCursorPosition(oldPosition, Console.CursorTop);
                            }
                        }
                    }
                }
            }
            catch (TaskCanceledException) { }
        }
        private static void DisplayOnlineMenu()
        {
            int choice = 0;
            string userchoice = "";
            while(!int.TryParse(userchoice, out choice) || choice < 1 || choice > 2)
            {
                Console.Clear();
                Console.WriteLine(userName);
                Console.WriteLine("Would you like to:\n1. Create a Game\n2. Join a game with a valid Game ID");
                userchoice = Console.ReadLine();
            }
            switch(choice)
            {
                case 1:
                    CreateGame(userToken);
                    break;
                case 2:
                    Console.WriteLine("Please enter the GameID");
                    gameID = Console.ReadLine();
                    JoinGame(userToken, gameID);
                    break;
            }
        }

        private static void PlayerTurn()
        {
            int diceRoll;
            if (currentPlayer.Geography == 1 && currentPlayer.Entertainment == 1 && currentPlayer.History == 1 && currentPlayer.Art == 1
                && currentPlayer.Science == 1 && currentPlayer.Sports == 1 && currentPlayer.CurrentPosition == 0)
            {
                finalRound();
            }
            Console.WriteLine(currentPlayer.userName + "'s Turn");
            Console.WriteLine("Rolling Dice");
            Thread.Sleep(1000);
            diceRoll = new Random().Next(1, 6);
            Console.WriteLine("You rolled a " + diceRoll);
            Thread.Sleep(1000);
            MovePlayer(diceRoll, currentPlayer.CurrentPosition);
            if (currentPlayer.CurrentPosition == blueStart && currentPlayer.Geography != 1 || currentPlayer.CurrentPosition == pinkStart && currentPlayer.Entertainment != 1
                    || currentPlayer.CurrentPosition == yellowStart && currentPlayer.History != 1
                    || currentPlayer.CurrentPosition == purpleStart && currentPlayer.Art != 1 ||
                    currentPlayer.CurrentPosition == greenStart && currentPlayer.Science != 1 || currentPlayer.CurrentPosition == orangeStart && currentPlayer.Sports != 1)
            {
                QuestionRound(currentPlayer.CurrentPosition, true);
            }
            else
            {
                QuestionRound(currentPlayer.CurrentPosition, false);
            }
        }


        private static void finalRound()
        {
            Random rng = new Random();
            ClearConsole();
            Console.WriteLine("The Final Round");
            int category = rng.Next(0, 5);
            string categoryS = "";
            switch (category)
            {
                case 0:
                    categoryS = "Geography";
                    break;
                case 1:
                    categoryS = "Entertainment";
                    break;
                case 2:
                    categoryS = "History";
                    break;
                case 3:
                    categoryS = "Art";
                    break;
                case 4:
                    categoryS = "Science";
                    break;
                case 5:
                    categoryS = "Sports/Leisure";
                    break;
            }
            Console.WriteLine("Your question category is " + categoryS + "\nGood Luck");
            AskQuestion(category);
            answerQuestion(false, category);
        }

        private static void QuestionRound(int currentPostition, bool isAPiece)
        {
            int category;
            
            if (currentPostition != 0)
            {
                AskQuestion(board[currentPostition].Category);
            }
            else
            {
                ClearConsole();
                Console.WriteLine("You are currently on the center which category would you like to play? Please enter the number" +
                    "\n1.Geography\n2.Entertainment\n3.History\n4.Art\n5.Science\n6.Sports\\Leisure");
                string userchoice = Console.ReadLine();
                while (!int.TryParse(userchoice, out category))
                {
                    Console.WriteLine("Incorrect Input please enter the number associated with the category");
                    Console.WriteLine("You are currently on the center which category would you like to play?" +
                    "\n1.Geography\n2.Entertainment\n3.History\n4.Art\n5.Science\n6.Sports\\Leisure");
                    userchoice = Console.ReadLine();
                }
                AskQuestion(category);
            }
            answerQuestion(isAPiece, board[currentPostition].Category);
        }
        private static async void answerQuestion(bool isAPiece, int category)
        {
            if (category != 6)
            {
                string userAnswser;
                string[] userAnswerA;
                string[] realAnswerA;
                int correctWords = 0;
                userAnswser = Console.ReadLine();
                char[] charsToTrim = { '.', '\"', ',' };
                userAnswerA = userAnswser.ToLower().Trim(charsToTrim).Split();
                realAnswerA = realAnswer.ToLower().Trim(charsToTrim).Split();
                if (realAnswerA.Length < userAnswerA.Length)
                {
                    ClearConsole();
                    Console.WriteLine("Incorrect Answer, The correct answer is:" + realAnswer);
                    Thread.Sleep(1000);
                    try
                    {
                        using (HttpClient client = CreateClient(URL))
                        {
                            tokenSource = new CancellationTokenSource();

                            dynamic user = new ExpandoObject();
                            user.userToken = userToken;
                            user.gameID = gameID;
                            user.category = category;
                            user.isAPiece = isAPiece;
                            user.answeredQuestion = 0;
                            user.playerTurn = playerTurn;

                            StringContent content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                            HttpResponseMessage response = await client.PutAsync("TriviaService/end-of-turn", content, tokenSource.Token);

                            if (response.IsSuccessStatusCode)
                            {
                                myTimerActive.Enabled = true;
                                return;
                            }
                        }
                    }
                    catch (TaskCanceledException) { }
                }
                for (int i = 0; i < userAnswerA.Length; i++)
                {
                    for (int j = 0; j < realAnswerA.Length; j++)
                    {
                        if (userAnswerA[i].Equals(realAnswerA[j]))
                        {
                            correctWords++;
                            break;
                        }
                    }
                }
                if (correctWords < realAnswerA.Length / 2)
                {
                    ClearConsole();
                    Console.WriteLine("Incorrect Answer, The correct answer is " + realAnswer);
                    Thread.Sleep(1000);
                    try
                    {
                        using (HttpClient client = CreateClient(URL))
                        {
                            tokenSource = new CancellationTokenSource();

                            dynamic user = new ExpandoObject();
                            user.userToken = userToken;
                            user.gameID = gameID;
                            user.category = category;
                            user.isAPiece = isAPiece;
                            user.answeredQuestion = 0;
                            user.playerTurn = playerTurn;

                            StringContent content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                            HttpResponseMessage response = await client.PutAsync("TriviaService/end-of-turn", content, tokenSource.Token);

                            if (response.IsSuccessStatusCode)
                            {
                                myTimerActive.Enabled = true;
                                return;
                            }
                        }
                    }
                    catch (TaskCanceledException) { }
                }
                else
                {
                    ClearConsole();
                    Console.WriteLine("Correct Good Job!");
                    Console.WriteLine("The Trivia Pursuit answer was " + realAnswer);
                    if (isAPiece)
                    {
                        Console.WriteLine("Congratulation " + currentPlayer.userName + " You gain a piece as well");
                    }
                    Thread.Sleep(1000);
                    try
                    {
                        using (HttpClient client = CreateClient(URL))
                        {
                            tokenSource = new CancellationTokenSource();

                            dynamic user = new ExpandoObject();
                            user.userToken = userToken;
                            user.gameID = gameID;
                            user.category = category;
                            user.isAPiece = isAPiece;
                            user.answeredQuestion = 1;
                            user.playerTurn = playerTurn;

                            StringContent content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                            HttpResponseMessage response = await client.PutAsync("TriviaService/end-of-turn", content, tokenSource.Token);

                            if (response.IsSuccessStatusCode)
                            {
                                myTimerActive.Enabled = true;
                                return;
                            }
                        }
                    }
                    catch (TaskCanceledException) { }
                }
            }
            else
            {
                Thread.Sleep(1000);
                try
                {
                    using (HttpClient client = CreateClient(URL))
                    {
                        tokenSource = new CancellationTokenSource();

                        dynamic user = new ExpandoObject();
                        user.userToken = userToken;
                        user.gameID = gameID;
                        user.category = category;
                        user.isAPiece = isAPiece;
                        user.answeredQuestion = 1;
                        user.playerTurn = playerTurn;

                        StringContent content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                        HttpResponseMessage response = await client.PutAsync("TriviaService/end-of-turn", content, tokenSource.Token);

                        if (response.IsSuccessStatusCode)
                        {
                            myTimerActive.Enabled = true;
                            return;
                        }
                    }
                }
                catch (TaskCanceledException) { }
            }
        }

        private static async void AskQuestion(int category)
        {
            try
            {
                using (HttpClient client = CreateClient(URL))
                {
                    dynamic card = new ExpandoObject();
                    card.gameID = gameID;
                    card.position = currentPlayer.CurrentPosition;
                    card.category = category;
                    card.playerMovement = currentPlayer.playerMovement;

                    tokenSource = new CancellationTokenSource();
                    StringContent content = new StringContent(JsonConvert.SerializeObject(card), Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync("TriviaService/cards-game", content, tokenSource.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        String result = await response.Content.ReadAsStringAsync();
                        dynamic currentCard = JsonConvert.DeserializeObject(result);
                        ClearConsole();
                        switch (category)
                        {
                            case 0:
                                Console.WriteLine(currentCard.Geography);
                                realAnswer = currentCard.GeographyA;
                                break;
                            case 1:
                                Console.WriteLine(currentCard.Entertainment);
                                realAnswer = currentCard.EntertainmentA;
                                break;
                            case 2:
                                Console.WriteLine(currentCard.History);
                                realAnswer = currentCard.HistoryA;
                                break;
                            case 3:
                                Console.WriteLine(currentCard.Art);
                                realAnswer = currentCard.ArtA;
                                break;
                            case 4:
                                Console.WriteLine(currentCard.Science);
                                realAnswer = currentCard.ScienceA;
                                break;
                            case 5:
                                Console.WriteLine(currentCard.Sports);
                                realAnswer = currentCard.SportsA;
                                break;
                        }
                    }
                }
            }
            catch (TaskCanceledException) { }
        }
        private static void MovePlayer(int spaces, int currentPosition)
        {
            if (currentPosition != 0)
            {
                BoardNode left;
                BoardNode right;
                BoardNode straight;
                BoardNode backwards;
                string choice;
                left = traverseLeft(spaces, currentPosition);
                right = traverseRight(spaces, currentPosition);
                straight = traverseStraight(spaces, currentPosition);
                backwards = traverseBackwards(spaces, currentPosition);
                Console.WriteLine("Choose a direction to move: Left, Right, Straight, Backwards or if their is that option Center");
                choice = Console.ReadLine();
                while (choice.ToLower().Equals("center") && straight.Category != -1 || choice.ToLower().Equals("left") && left == null || choice.ToLower().Equals("right") && right == null
                    || choice.ToLower().Equals("straight") && straight.Category == -1 || choice.ToLower().Equals("backwards") && backwards == null || choice.ToLower() != "left"
                    && choice.ToLower() != "right" && choice.ToLower() != "straight" && choice.ToLower() != "backwards" && choice.ToLower() != "center")
                {
                    Console.WriteLine("Incorrect Input please make sure you are choose a direction with a valid tile");
                    Console.WriteLine("Choose a direction to move: Left, Right, Straight, Backwards or if their is that option Center");
                    choice = Console.ReadLine();
                }
                switch (choice.ToLower())
                {
                    case "straight":
                        currentPlayer.CurrentPosition = straight.position;
                        currentPlayer.playerMovement = "S" + spaces;
                        break;
                    case "left":
                        currentPlayer.CurrentPosition = left.position;
                        currentPlayer.playerMovement = "L" + spaces;
                        break;
                    case "right":
                        currentPlayer.CurrentPosition = right.position;
                        currentPlayer.playerMovement = "R" + spaces;
                        break;
                    case "backwards":
                        currentPlayer.CurrentPosition = backwards.position;
                        currentPlayer.playerMovement = "B" + spaces;
                        break;
                    case "center":
                        if (straight.position != 0)
                        {
                            currentPlayer.playerMovement = "C" + (spaces - straight.position) +".";
                            currentPlayer.CurrentPosition = traversePaths(straight.position).position;
                        }
                        break;
                }
            }
            else
            {
                currentPlayer.CurrentPosition = traversePaths(spaces).position;
            }
        }
        private static BoardNode traversePaths(int spaces)
        {
            Console.WriteLine("You are at the Center");
            BoardNode blue = traverseAllPaths(spaces, 1);
            BoardNode pink = traverseAllPaths(spaces, 7);
            BoardNode yellow = traverseAllPaths(spaces, 13);
            BoardNode purple = traverseAllPaths(spaces, 19);
            BoardNode green = traverseAllPaths(spaces, 25);
            BoardNode orange = traverseAllPaths(spaces, 31);
            Console.WriteLine("Would you like to go down the Blue, Pink, Yellow, Purple, Green, or Orange Path?");
            string choice = Console.ReadLine();
            while (!choice.ToLower().Equals("blue") || !choice.ToLower().Equals("pink") || !choice.ToLower().Equals("yellow") || !choice.ToLower().Equals("green") || !choice.ToLower().Equals("orange"))
            {
                Console.Clear();
                Console.WriteLine("Incorrect input");
                Console.WriteLine("You are at the center would you like to go down the Blue, Pink, Yellow, Purple, Green, or Orange Path?");
                choice = Console.ReadLine();
            }
            switch (choice.ToLower())
            {
                case "blue":
                    currentPlayer.playerMovement = currentPlayer.playerMovement + "BP" + spaces;
                    return blue;
                case "pink":
                    currentPlayer.playerMovement = currentPlayer.playerMovement + "PiP" + spaces;
                    return pink;
                case "yellow":
                    currentPlayer.playerMovement = currentPlayer.playerMovement + "YP" + spaces;
                    return yellow;
                case "purple":
                    currentPlayer.playerMovement = currentPlayer.playerMovement + "PuP" + spaces;
                    return purple;
                case "green":
                    currentPlayer.playerMovement = currentPlayer.playerMovement + "GP" + spaces;
                    return green;
                case "orange":
                    currentPlayer.playerMovement = currentPlayer.playerMovement + "OP" + spaces;
                    return orange;
            }
            return null;
        }
        private static BoardNode traverseAllPaths(int spaces, int startposition)
        {
            string category = "";
            string path = "";
            switch (board[startposition + (spaces - 1)].Category)
            {
                case 0:
                    category = "Geography";
                    break;
                case 1:
                    category = "Entertainment";
                    break;
                case 2:
                    category = "History";
                    break;
                case 3:
                    category = "Art";
                    break;
                case 4:
                    category = "Science";
                    break;
                case 5:
                    category = "Sports/Leisure";
                    break;
                case 6:
                    category = "roll again";
                    break;
            }
            switch (startposition)
            {
                case 1:
                    path = "Blue Path";
                    break;
                case 7:
                    path = "Pink Path";
                    break;
                case 13:
                    path = "Yellow Path";
                    break;
                case 19:
                    path = "Purple Path";
                    break;
                case 25:
                    path = "Green Path";
                    break;
                case 31:
                    path = "Orange Path";
                    break;
            }
            Console.WriteLine("Moving Down the " + path + " " + spaces + " spaces will land you on the tile with " + category);
            return board[startposition + (spaces - 1)];
        }
        private static BoardNode traverseLeft(int spaces, int currentPosition)
        {
            BoardNode currentNode = board[currentPosition];
            string category = "";
            for (int i = 0; i < spaces; i++)
            {
                if (currentNode.left == null)
                {
                    Console.WriteLine("You can't traverse Left");
                    return null;
                }
                else
                {
                    currentNode = currentNode.left;
                }
            }
            switch (currentNode.Category)
            {
                case 0:
                    category = "Geography";
                    break;
                case 1:
                    category = "Entertainment";
                    break;
                case 2:
                    category = "History";
                    break;
                case 3:
                    category = "Art";
                    break;
                case 4:
                    category = "Science";
                    break;
                case 5:
                    category = "Sports/Leisure";
                    break;
                case 6:
                    category = "roll again";
                    break;
            }
            if (currentNode.position == blueStart || currentNode.position == pinkStart || currentNode.position == yellowStart || currentNode.position == purpleStart || currentNode.position == greenStart || currentNode.position == orangeStart)
            {
                Console.WriteLine("Moving Left " + spaces + " spaces will land you on the tile for a piece with " + category);
            }
            else
            {
                Console.WriteLine("Moving Left " + spaces + " spaces will land you on the tile with " + category);
            }
            return currentNode;
        }

        private static BoardNode traverseRight(int spaces, int currentPosition)
        {
            BoardNode currentNode = board[currentPosition];
            string category = "";
            for (int i = 0; i < spaces; i++)
            {
                if (currentNode.right == null)
                {
                    Console.WriteLine("You can't traverse Right");
                    return null;
                }
                else
                {
                    currentNode = currentNode.right;
                }
            }
            switch (currentNode.Category)
            {
                case 0:
                    category = "Geography";
                    break;
                case 1:
                    category = "Entertainment";
                    break;
                case 2:
                    category = "History";
                    break;
                case 3:
                    category = "Art";
                    break;
                case 4:
                    category = "Science";
                    break;
                case 5:
                    category = "Sports/Leisure";
                    break;
                case 6:
                    category = "roll again";
                    break;
            }
            if (currentNode.position == blueStart || currentNode.position == pinkStart || currentNode.position == yellowStart || currentNode.position == purpleStart || currentNode.position == greenStart || currentNode.position == orangeStart)
            {
                Console.WriteLine("Moving Right " + spaces + " spaces will land you on the tile for a piece with " + category);
            }
            else
            {
                Console.WriteLine("Moving Right " + spaces + " spaces will land you on the tile with " + category);
            }
            return currentNode;
        }

        private static BoardNode traverseStraight(int spaces, int currentPosition)
        {
            BoardNode currentNode = board[currentPosition];
            string category = "";
            for (int i = 0; i < spaces; i++)
            {
                if (currentNode.myType() == 1)
                {
                    Console.WriteLine("You can move " + i + " spaces to the Center");
                    return new BoardNode(0) { position = spaces - i, Category = -1 };
                }
                if (currentNode.straight == null)
                {
                    Console.WriteLine("You can't traverse Straight");
                    return null;
                }
                currentNode = currentNode.straight;
            }
            switch (currentNode.Category)
            {
                case 0:
                    category = "Geography";
                    break;
                case 1:
                    category = "Entertainment";
                    break;
                case 2:
                    category = "History";
                    break;
                case 3:
                    category = "Art";
                    break;
                case 4:
                    category = "Science";
                    break;
                case 5:
                    category = "Sports/Leisure";
                    break;
                case 6:
                    category = "roll again";
                    break;
            }
            if (currentNode.position == 0)
            {
                Console.WriteLine("Moving Straight " + spaces + " spaces will land you on the Center tile");
            }
            else
            {
                Console.WriteLine("Moving Straight " + spaces + " spaces will land you on the tile with " + category);
            }

            return currentNode;
        }

        private static BoardNode traverseBackwards(int spaces, int currentPosition)
        {
            BoardNode currentNode = board[currentPosition];
            string category = "";
            for (int i = 0; i < spaces; i++)
            {
                if (currentNode.backwards == null)
                {
                    Console.WriteLine("You can't traverse Backwards");
                    return null;
                }
                else
                {
                    currentNode = currentNode.backwards;
                }
            }
            switch (currentNode.Category)
            {
                case 0:
                    category = "Geography";
                    break;
                case 1:
                    category = "Entertainment";
                    break;
                case 2:
                    category = "History";
                    break;
                case 3:
                    category = "Art";
                    break;
                case 4:
                    category = "Science";
                    break;
                case 5:
                    category = "Sports/Leisure";
                    break;
                case 6:
                    category = "roll again";
                    break;
            }
            if (currentNode.position == blueStart || currentNode.position == pinkStart || currentNode.position == yellowStart || currentNode.position == purpleStart || currentNode.position == greenStart || currentNode.position == orangeStart)
            {
                Console.WriteLine("Moving Backwards " + spaces + " spaces will land you on the tile for a piece with " + category);
            }
            else
            {
                Console.WriteLine("Moving Backwards " + spaces + " spaces will land you on the tile with " + category);
            }
            return currentNode;
        }

        private static void CreateBoard()
        {
            int count = 0;
            board = new Dictionary<int, BoardNode>();
            board.Add(0, new CenterNode());
            board[0].position = 0;
            CreateBluePath(ref count);
            CreatePinkPath(ref count);
            CreateYellowPath(ref count);
            CreatePurplePath(ref count);
            CreateGreenPath(ref count);
            CreateOrangePath(ref count);
            CreateRestofBoard(ref count);
        }
        private static void CreateBluePath(ref int count)
        {
            count++;
            board.Add(count, new BoardNode(0) { straight = board[0], Category = 3 });
            board[count].position = count;
            CenterNode n = (CenterNode)board[0];
            n.BluePath = board[count];
            board[count].backwards = board[0];
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 5 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 2 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 4 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 1 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 0 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            blueStart = count;
        }

        private static void CreatePinkPath(ref int count)
        {
            count++;
            board.Add(count, new BoardNode(0) { straight = board[0], Category = 2 });
            CenterNode n = (CenterNode)board[0];
            n.PinkPath = board[count];
            board[count].backwards = board[0];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 4 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 3 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 5 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 0 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 2 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            pinkStart = count;
        }

        private static void CreateYellowPath(ref int count)
        {
            count++;
            board.Add(count, new BoardNode(0) { straight = board[0], Category = 5 });
            CenterNode n = (CenterNode)board[0];
            n.YellowPath = board[count];
            board[count].backwards = board[0];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 1 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 4 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 0 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 3 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 2 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            yellowStart = count;
        }

        private static void CreatePurplePath(ref int count)
        {
            count++;
            board.Add(count, new BoardNode(0) { straight = board[0], Category = 4 });
            CenterNode n = (CenterNode)board[0];
            n.PurplePath = board[count];
            board[count].backwards = board[0];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 0 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 5 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 1 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 2 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 3 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            purpleStart = count;
        }

        private static void CreateGreenPath(ref int count)
        {
            count++;
            board.Add(count, new BoardNode(0) { straight = board[0], Category = 1 });
            CenterNode n = (CenterNode)board[0];
            n.GreenPath = board[count];
            board[count].backwards = board[0];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 3 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 0 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 2 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 5 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 4 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            greenStart = count;
        }

        private static void CreateOrangePath(ref int count)
        {
            count++;
            board.Add(count, new BoardNode(0) { straight = board[0], Category = 0 });
            CenterNode n = (CenterNode)board[0];
            n.OrangePath = board[count];
            board[count].backwards = board[0];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 2 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 1 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 3 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 4 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { straight = board[count - 1], Category = 5 });
            board[count - 1].backwards = board[count];
            board[count].position = count;
            orangeStart = count;
        }
        private static void CreateRestofBoard(ref int count)
        {
            count++;
            board.Add(count, new BoardNode(0) { left = board[blueStart], Category = 1 });
            board[blueStart].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 6 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 4 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 5 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 6 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], right = board[purpleStart], Category = 2 });
            board[count - 1].right = board[count];
            board[count].position = count;

            count++;
            board.Add(count, new BoardNode(0) { left = board[purpleStart], Category = 2 });
            board[purpleStart].right = board[count];
            board[purpleStart].left = board[count - 1];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 6 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 1 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 0 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 6 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], right = board[greenStart], Category = 5 });
            board[count - 1].right = board[count];
            board[count].position = count;

            count++;
            board.Add(count, new BoardNode(0) { left = board[greenStart], Category = 5 });
            board[greenStart].right = board[count];
            board[greenStart].left = board[count - 1];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 6 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 2 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 3 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 6 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], right = board[pinkStart], Category = 0 });
            board[count - 1].right = board[count];
            board[count].position = count;

            count++;
            board.Add(count, new BoardNode(0) { left = board[pinkStart], Category = 0 });
            board[pinkStart].right = board[count];
            board[pinkStart].left = board[count - 1];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 6 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 5 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 4 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 6 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], right = board[yellowStart], Category = 3 });
            board[count - 1].right = board[count];
            board[count].position = count;

            count++;
            board.Add(count, new BoardNode(0) { left = board[yellowStart], Category = 3 });
            board[yellowStart].right = board[count];
            board[yellowStart].left = board[count - 1];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 6 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 0 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 1 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 6 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], right = board[orangeStart], Category = 4 });
            board[count - 1].right = board[count];
            board[count].position = count;

            count++;
            board.Add(count, new BoardNode(0) { left = board[orangeStart], Category = 4 });
            board[orangeStart].right = board[count];
            board[orangeStart].left = board[count - 1];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 6 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 3 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 2 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], Category = 6 });
            board[count - 1].right = board[count];
            board[count].position = count;
            count++;
            board.Add(count, new BoardNode(0) { left = board[count - 1], right = board[blueStart], Category = 1 });
            board[count - 1].right = board[count];
            board[blueStart].left = board[count];
            board[count].position = count;
        }
        private static void ClearConsole()
        {
            Console.SetCursorPosition(0, 4);
            Console.Write(new String(' ', Console.BufferWidth));
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.WriteLine(currentPlayer.userName);
            Console.Write(new String(' ', Console.BufferWidth));
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.WriteLine("Geography:" + currentPlayer.Geography + " Entertainment:" + currentPlayer.Entertainment + " History:" + currentPlayer.History +
                " Art:" + currentPlayer.Art + " Science:" + currentPlayer.Science + " Sports/Leisure:" + currentPlayer.Sports);
        }

        public static HttpClient CreateClient(string url)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            return client;
        }
    }
}
