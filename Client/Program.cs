using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.Security.Principal;
using System.Net.Configuration;
using System.Reflection;
using System.Net.NetworkInformation;
using System.Security.Cryptography;

namespace MyClient
{
    class Program
    {
        static private TcpClient client = null;
        static private NetworkStream stream = null;

        static void Main(string[] args)
        {

            string currentLogin = "";

            string msg = ""; // для сообщений
            string login = ""; // для логина
            string password = ""; // для пароля
            string email = ""; // для почты
            string address = "127.0.0.1"; // для адреса7
            int port = 111; // для порта
            int key = 0; // временный ключ

            //Console.WriteLine("<System>: Enter address:");
            //address = Console.ReadLine();
            //Console.WriteLine("<System>: Enter port:");
            //port = Convert.ToInt32(Console.ReadLine());

            int status = 0; // состояние клиента

            // ввести пароль
            // получить соль
            // хеш(пароль + соль)
            // отправить хеш для сверки на сервер

            while (true)
            {
                try
                {

                    // Определение типа
                    do
                    {
                        if (status == 0 || status == -1) // Вход не выыполнен
                        {
                            Console.WriteLine("1. Вход\n2. Регистрация");
                            int case_ = Convert.ToInt32(Console.ReadLine());
                            switch (case_)
                            {
                                case 1:  // Вход в аккаунт

                                    // Ввод логина
                                    Console.WriteLine("<System>: Enter login:");
                                    login = Console.ReadLine();
                                    // Ввод пароля
                                    Console.WriteLine("<System>: Enter password:");
                                    password = Console.ReadLine();
                                    // Подключение к серверу
                                    if (!ConnectToServer(address, port))
                                    {
                                        Console.WriteLine("<System>: Error connection!");
                                        status = -1;
                                        break;
                                    }

                                    SendMessage(stream, "log"); // Отправка типа
                                    ReadMessage(stream); // Получение ответа {System}
                                    SendMessage(stream, login); // Отправка логина
                                    msg = ReadMessage(stream); // Получение ответа (salt or error)
                                    if (msg.Contains("{ER1}"))
                                    {
                                        Console.WriteLine(msg.Substring(5));
                                        status = -1;
                                        break;
                                    }
                                    SendMessage(stream, ComputePasswordHash(password, msg)); // отправление хеша
                                    msg = ReadMessage(stream);

                                    if (msg.Contains("{ER1}"))
                                    {
                                        Console.WriteLine(msg.Substring(5));
                                        status = -1;
                                    }
                                    else
                                    {
                                        currentLogin = login;
                                        key = Convert.ToInt32(msg); // Конвертирование ключа из string в int
                                        Console.WriteLine("<Server>: KEY = " + key.ToString());
                                        status = 1; // запоминаем состояние о том, что клиент вошел в аккаунт
                                    }

                                    // разрываем связь между сервером и клиентом
                                    stream.Close();
                                    client.Close();


                                    break;
                                case 2: // Регистрация

                                    Console.WriteLine("<System>: Enter login (4 and more chars):");
                                    login = Console.ReadLine();
                                    if (login.Length < 4)
                                    {
                                        Console.WriteLine("<System>: Login's length must be more than 3 chars!");
                                        break;
                                    }

                                    Console.WriteLine("<System>: Enter password (8 and more chars):");
                                    password = Console.ReadLine();
                                    //if (password.Length < 8)
                                    //{
                                    //    Console.WriteLine("<System>: Password's length must be more than 7 chars!");
                                    //    break;
                                    //}

                                    Console.WriteLine("<System>: Enter email:");
                                    email = Console.ReadLine();

                                    // Подключение к серверу
                                    if (!ConnectToServer(address, port))
                                    {
                                        Console.WriteLine("<System>: Error connection!");
                                        status = -1;
                                        break;
                                    }

                                    SendMessage(stream, "reg"); // Отправка типа
                                    ReadMessage(stream); // Получение ответа {System}
                                    SendMessage(stream, login); // Отправка логина
                                    ReadMessage(stream); // Получение ответа {System}
                                    // отправка хеша пароля
                                    string salt = GenSalt(16);
                                    string hash = ComputePasswordHash(password, salt);
                                    SendMessage(stream, hash);
                                    ReadMessage(stream); // Получение ответа {System}
                                    SendMessage(stream, salt);
                                    ReadMessage(stream); // Получение ответа {System}
                                    SendMessage(stream, email); // Отправка логина


                                    msg = ReadMessage(stream);
                                    if (msg.Contains("{ER1}"))
                                    {
                                        Console.WriteLine(msg.Substring(5));
                                        status = -1;
                                    }
                                    else if (msg.Contains("{INF}"))
                                    {
                                        Console.WriteLine(msg.Substring(5));
                                        status = 0;
                                    }

                                    stream.Close();
                                    client.Close();

                                    status = 0; // состояние ожидания 

                                    break;

                                default:
                                    Console.WriteLine("<System>: ERROR: This case is not founded.");
                                    status = -1; // состояние ошибки
                                    break;
                            }
                        }
                        else if (status == 1 || status == -2) // если уже выполнен вход в аккаунт
                        {
                            Console.WriteLine("1. Отправка Сообщения\n" +
                                "2. Проверка входящих сообщений\n" +
                                "3. Отправить запрос на дружбу\n" +
                                "4. Проверить входящие запросы на дружбу\n" +
                                "5. Посмотреть исходящие запросы на дружбу\n" +
                                "6. Принять запрос\n" +
                                "7. Отклонить входящий запрос\n" +
                                "8. Отменить исходящий запрос\n" +
                                "9. Показать добавленных друзей\n" +
                                "10. Удалить из друзей\n" +
                                "0. Выход из аккаунта");
                            int case_ = Convert.ToInt32(Console.ReadLine());
                            switch (case_)
                            {
                                case 1:  // Отправка сообщения

                                    // Ввод Логина для кого посылать сообщение
                                    Console.WriteLine("<System>: Enter reciever's login:");
                                    login = Console.ReadLine();
                                    if (login == currentLogin)
                                    {
                                        Console.WriteLine("<System>: You can't sumbit message to yourself:");
                                        status = 1;
                                        break;
                                    }

                                    // Ввод сообщения максимум 256 символов
                                    Console.WriteLine("<System>: Enter message ( <= 255 chars):");
                                    msg = Console.ReadLine();

                                    ConnectToServer(address, port); // Подключение к серверу
                                    SendMessage(stream, "send"); // Послание сообщения 
                                    ReadMessage(stream); // Чтение
                                    SendMessage(stream, key.ToString()); // Послание ключа
                                    ReadMessage(stream); // Получение сообщения {Sys}
                                    SendMessage(stream, login); // Послание логина, кому нужно послать сообщения
                                    ReadMessage(stream);
                                    SendMessage(stream, msg); // Послание сообщения

                                    msg = ReadMessage(stream);
                                    if (msg.Contains("{ER1}"))
                                    {
                                        Console.WriteLine(msg.Substring(5));
                                        status = 1;
                                    }
                                    else if (msg.Contains("{INF}"))
                                    {
                                        Console.WriteLine("<Server>: Success! Message has been sent!");
                                        status = 1;
                                    }

                                    stream.Close();
                                    client.Close();

                                    status = 1;
                                    break;
                                case 2: // Просмотр входящих сообщений

                                    Console.WriteLine("<System>: Enter sender's login:");
                                    login = Console.ReadLine();
                                    if (login == currentLogin)
                                    {
                                        Console.WriteLine("<System>: You can't sumbit message to yourself:");
                                        status = 1;
                                        break;
                                    }

                                    ConnectToServer(address, port); // Подключение к серверу
                                    SendMessage(stream, "messages");
                                    ReadMessage(stream); // Чтение
                                    SendMessage(stream, key.ToString());
                                    ReadMessage(stream); // Получение сообщения {Sys}
                                    SendMessage(stream, login); // Послание логина, кому нужно послать сообщения

                                    msg = ReadMessage(stream);
                                    if (msg.Contains("{ER1}"))
                                    {
                                        Console.WriteLine(msg.Substring(5));
                                        status = 1;
                                    }
                                    else if (msg.Contains("{INF}"))
                                    {
                                        Console.WriteLine(msg.Substring(5));
                                        status = 1;
                                    }

                                    stream.Close();
                                    client.Close();

                                    status = 1;
                                    break;
                                case 3: // Отправить запрос на дружбу

                                    Console.WriteLine("<System>: Enter friend's login:");
                                    login = Console.ReadLine();
                                    if (login == currentLogin)
                                    {
                                        Console.WriteLine("<System>: You can't sumbit request to yourself:");
                                        status = 1;
                                        break;
                                    }

                                    ConnectToServer(address, port); // Подключение к серверу
                                    SendMessage(stream, "add_friend");
                                    ReadMessage(stream); // Чтение
                                    SendMessage(stream, key.ToString());
                                    ReadMessage(stream); // Получение сообщения {Sys}
                                    SendMessage(stream, login); // Послание логина, кому нужно послать сообщения

                                    msg = ReadMessage(stream);
                                    if (msg.Contains("{ER1}"))
                                    {
                                        Console.WriteLine(msg.Substring(5));
                                        status = 1;
                                    }
                                    else if (msg.Contains("{INF}"))
                                    {
                                        Console.WriteLine(msg.Substring(5));
                                        status = 1;
                                    }

                                    stream.Close();
                                    client.Close();

                                    break;
                                case 4: // Просмотр входящих запросов на дружбу

                                    ConnectToServer(address, port); // Подключение к серверу
                                    SendMessage(stream, "check_requests");
                                    ReadMessage(stream);
                                    SendMessage(stream, key.ToString());

                                    msg = ReadMessage(stream);
                                    if (msg.Contains("{INF}"))
                                    {
                                        Console.WriteLine("<Server>: Incoming requests:\n" + msg.Substring(5));
                                    }
                                    else if (msg.Contains("{ER1}"))
                                    {
                                        Console.WriteLine("<Server>: No incoming requests.");
                                    }

                                    stream.Close();
                                    client.Close();

                                    status = 1;
                                    break;
                                case 5: // Просмотр исходящих запросов на дружбу

                                    ConnectToServer(address, port); // Подключение к серверу
                                    SendMessage(stream, "check_outgoing");
                                    ReadMessage(stream);
                                    SendMessage(stream, key.ToString());

                                    msg = ReadMessage(stream);
                                    if (msg.Contains("{INF}"))
                                    {
                                        Console.WriteLine("<Server>: Outgoing requests:\n" + msg.Substring(5));
                                    }
                                    else if (msg.Contains("{ER1}"))
                                    {
                                        Console.WriteLine("<Server>: No outgoing requests.");
                                    }

                                    stream.Close();
                                    client.Close();

                                    status = 1;
                                    break;
                                case 6: // Принять запрос

                                    // логин друга
                                    Console.WriteLine("<System>: Enter sender's login:");
                                    login = Console.ReadLine();
                                    if (login == currentLogin)
                                    {
                                        Console.WriteLine("<System>: You can't write your login!");
                                        status = 1;
                                        break;
                                    }

                                    ConnectToServer(address, port); // Подключение к серверу
                                    SendMessage(stream, "accept_request");
                                    ReadMessage(stream);
                                    SendMessage(stream, key.ToString());
                                    ReadMessage(stream);
                                    SendMessage(stream, login);

                                    msg = ReadMessage(stream);
                                    if (msg.Contains("{INF}"))
                                    {
                                        Console.WriteLine(msg.Substring(5));
                                    }
                                    else if (msg.Contains("{ER1}"))
                                    {
                                        Console.WriteLine(msg.Substring(5));
                                    }

                                    stream.Close();
                                    client.Close();
                                    status = 1;
                                    break;
                                case 7: // Отклонить входящий запрос

                                    // логин пользователя, кидающего запрос
                                    Console.WriteLine("<System>: Enter reciever's login:");
                                    login = Console.ReadLine();
                                    if (login == currentLogin)
                                    {
                                        Console.WriteLine("<System>: You can't write your login!");
                                        status = 1;
                                        break;
                                    }

                                    ConnectToServer(address, port); // Подключение к серверу
                                    SendMessage(stream, "decline_incoming");
                                    ReadMessage(stream);
                                    SendMessage(stream, key.ToString());
                                    ReadMessage(stream);
                                    SendMessage(stream, login);
                                    msg = ReadMessage(stream);
                                    if (msg.Contains("{INF}"))
                                    {
                                        Console.WriteLine(msg.Substring(5));
                                    }
                                    else if (msg.Contains("{ER1}"))
                                    {
                                        Console.WriteLine(msg.Substring(5));
                                    }

                                    stream.Close();
                                    client.Close();

                                    status = 1;
                                    break;
                                case 8: // Отменить исходящий запрос

                                    // логин друга
                                    Console.WriteLine("<System>: Enter reciever's login:");
                                    login = Console.ReadLine();
                                    if (login == currentLogin)
                                    {
                                        Console.WriteLine("<System>: You can't write your login!");
                                        status = 1;
                                        break;
                                    }

                                    ConnectToServer(address, port); // Подключение к серверу
                                    SendMessage(stream, "cancel_outgoing");
                                    ReadMessage(stream);
                                    SendMessage(stream, key.ToString());
                                    ReadMessage(stream);
                                    SendMessage(stream, login);

                                    msg = ReadMessage(stream);
                                    if (msg.Contains("{INF}"))
                                    {
                                        Console.WriteLine(msg.Substring(5));
                                    }
                                    else if (msg.Contains("{ER1}"))
                                    {
                                        Console.WriteLine(msg.Substring(5));
                                    }

                                    stream.Close();
                                    client.Close();
                                    status = 1;
                                    break;
                                case 9: // показать друзей
                                    ConnectToServer(address, port); // Подключение к серверу
                                    SendMessage(stream, "show_friends");
                                    ReadMessage(stream);
                                    SendMessage(stream, key.ToString());

                                    msg = ReadMessage(stream);
                                    if (msg.Contains("{INF}"))
                                    {
                                        Console.WriteLine("<Server>: Friends:\n" + msg.Substring(5));
                                    }
                                    else if (msg.Contains("{ER1}"))
                                    {
                                        Console.WriteLine("<Server>: You don't have any friends yet.");
                                    }

                                    stream.Close();
                                    client.Close();
                                    break;
                                case 10: // Удалить из друзей

                                    Console.WriteLine("<System>: Enter reciever's login:");
                                    login = Console.ReadLine();
                                    if (login == currentLogin)
                                    {
                                        Console.WriteLine("<System>: You can't write your login!");
                                        status = 1;
                                        break;
                                    }

                                    ConnectToServer(address, port); // Подключение к серверу
                                    SendMessage(stream, "delete_friend");
                                    ReadMessage(stream);
                                    SendMessage(stream, key.ToString());
                                    ReadMessage(stream);
                                    SendMessage(stream, login);

                                    msg = ReadMessage(stream);
                                    if (msg.Contains("{INF}"))
                                    {
                                        Console.WriteLine(msg.Substring(5));
                                    }
                                    else if (msg.Contains("{ER1}"))
                                    {
                                        Console.WriteLine(msg.Substring(5));
                                    }

                                    stream.Close();
                                    client.Close();
                                    status = 1;
                                    break;
                                case 0: // Выход из аккаунта
                                    if (!ConnectToServer(address, port))
                                    {
                                        Console.WriteLine("<System>: Error connection!");
                                        status = -1;
                                        break;
                                    }

                                    SendMessage(stream, "exit"); // Отправка типа
                                    ReadMessage(stream); // Получение ответа {System}
                                    SendMessage(stream, key.ToString()); // Отправка логина
                                    key = 0; // удаление ключа
                                    currentLogin = "";
                                    status = 0;

                                    stream.Close();
                                    client.Close();
                                    break;
                                default:
                                    Console.WriteLine("<System>: ERROR: This case is not founded.");
                                    status = -1;
                                    break;
                            }
                        }
                        else // ошибка
                        {
                            Console.WriteLine("Fatal error");
                        }

                    } while (status == -1);
                }
                catch (Exception e)
                {
                    Console.WriteLine("<System>: Exception:" + e.Message);
                }
            }

        }

        // Метод подключается к серверу и возвращает true в случае удачного подключения и false в случае неудачи
        static bool ConnectToServer(string address, int port)
        {
            try
            {
                // Подключение к серверу
                client = new TcpClient(address, port);
                stream = client.GetStream();
                return true;
            }
            catch
            {
                return false;
            }
        }

        static void SendMessage(NetworkStream stream, string msg)
        {
            stream.Write(Encoding.UTF8.GetBytes(msg), 0, Encoding.UTF8.GetBytes(msg).Length);
        }
        static string ReadMessage(NetworkStream stream)
        {
            byte[] buff = new byte[255];
            stream.Read(buff, 0, buff.Length);

            string s = Encoding.UTF8.GetString(buff);
            string t = "";
            for (int i = 0; s[i] != '\0'; i++)
            {
                t += s[i];
            }
            return t;
        }

        static protected string GenSalt(int length)
        {
            RNGCryptoServiceProvider p = new RNGCryptoServiceProvider();
            var salt = new byte[length];
            p.GetBytes(salt);
            return Convert.ToBase64String(salt);
        }

        static protected string ComputePasswordHash(string password, string salt)
        {
            SHA256 SHA = new SHA256Managed();

            if (password == null || salt == null)
                return null;
            return BitConverter.ToString(SHA.ComputeHash(Encoding.UTF8.GetBytes(password + salt))).Replace("-", "").ToLower();
        }
    }
}
