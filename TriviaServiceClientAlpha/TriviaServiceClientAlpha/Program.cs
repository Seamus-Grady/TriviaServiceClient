using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using System.Dynamic;

namespace TriviaServiceClientAlpha
{
    class Program
    {
        private static System.Timers.Timer myTimer = new System.Timers.Timer();
        private static System.Timers.Timer myTimerActive = new System.Timers.Timer();

        private static string URL = "http://localhost:60000/";

        private static CancellationTokenSource tokenSource;
        private static Dictionary<int, BoardNode> board;
        private static int purpleStart;
        private static int orangeStart;
        private static int greenStart;
        private static int pinkStart;
        private static int blueStart;
        private static int yellowStart;
        private static string userToken;
        private static string gameID;
        private static string playerTurn;
        private static  string userName;
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
                        else
                        {
                            myTimerActive.Interval = 1000;
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
        private static async void DisplayLobby(Object myObject, EventArgs myEventArgs, bool isHost)
        {
            try
            {
                using (HttpClient client = CreateClient(URL))
                {
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
