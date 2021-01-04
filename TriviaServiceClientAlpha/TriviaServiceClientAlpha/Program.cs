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
        private static string userToken;
        private static string gameID;
        private static string playerTurn;
        private static  Player currentPlayer;
        private static Task endlessTask;
        static void Main(string[] args)
        {
            currentPlayer = new Player();
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
                        currentPlayer.userName = nickname;
                        ClearConsole();
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
                    HttpResponseMessage response = await client.PostAsync("TriviaService/create-game", content, tokenSource.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        playerTurn = (string)JsonConvert.DeserializeObject(result);
                        ClearConsole();

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
                        ClearConsole();

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
        private static void DisplayOnlineMenu()
        {
            int choice = 0;
            string userchoice = "";
            while(!int.TryParse(userchoice, out choice) || choice < 1 || choice > 2)
            {
                Console.Clear();
                Console.WriteLine(currentPlayer.userName);
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
        private static void ClearConsole()
        {
            Console.Clear();
            Console.WriteLine(currentPlayer.userName);
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
