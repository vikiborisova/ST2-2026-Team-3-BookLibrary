using BookLibrary.Models;

namespace BookLibrary.Data
{
    public class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            // Проверяваме дали вече има потребители в базата
            if (context.Users.Any())
            {
                return; // Базата вече е запълнена
            }

            // 1. Потребители
            var users = new[]
            {
                new User { Username = "user1-test", Password = "password" },
                new User { Username = "user2-test", Password = "password" },
                new User { Username = "user3-test", Password = "password" },
                new User { Username = "user4-test", Password = "password" },
                new User { Username = "user5-test", Password = "password" }
            };

            context.Users.AddRange(users);
            context.SaveChanges(); // Записваме ги, за да получат ID-та

            // 2. Книги
            var books = new[]
            {
                // Мария (Id = 1)
                new Book { Title = "Под игото", Author = "Иван Вазов", Genre = "Класически роман", Year = 1894, Status = "прочетена", PdfPath = "/pdfs/pod_igoto.pdf", Description = "Класически български исторически роман...", UserId = users[0].Id },
                new Book { Title = "Тютюн", Author = "Димитър Димов", Genre = "Психологически роман", Year = 1951, Status = "прочетена", PdfPath = "/pdfs/tyutyun.pdf", Description = "Монументална творба за упадъка...", UserId = users[0].Id },
                new Book { Title = "Малкият принц", Author = "Антоан дьо Сент-Екзюпери", Genre = "Философска приказка", Year = 1943, Status = "прочетена", PdfPath = "/pdfs/malkiyat_prints.pdf", Description = "Поетична история за приятелството...", UserId = users[0].Id },
                new Book { Title = "Шепотът на вятъра", Author = "Николай Теллалов", Genre = "Българска фантастика", Year = 1998, Status = "в процес", PdfPath = "/pdfs/shepotat_na_vyatra.pdf", Description = "Завладяваща фантастична история...", UserId = users[0].Id },
                new Book { Title = "Алхимикът", Author = "Паулу Коелю", Genre = "Приключенски", Year = 1988, Status = "ще чета", PdfPath = "/pdfs/alhimikat.pdf", Description = "Роман за следването на личната легенда...", UserId = users[0].Id },

                // Георги (Id = 2)
                new Book { Title = "Време разделно", Author = "Антон Дончев", Genre = "Исторически роман", Year = 1964, Status = "прочетена", PdfPath = "/pdfs/vrime_razdelno.pdf", Description = "Драматична творба за съдбите в Родопите...", UserId = users[1].Id },
                new Book { Title = "Престъпление и наказание", Author = "Фьодор Достоевски", Genre = "Психологически трилър", Year = 1866, Status = "прочетена", PdfPath = "/pdfs/prestaplenie_i_nakazanie.pdf", Description = "Анализ на човешката съвест и вина...", UserId = users[1].Id },
                new Book { Title = "1984", Author = "Джордж Оруел", Genre = "Антиутопия", Year = 1949, Status = "в процес", PdfPath = "/pdfs/1984.pdf", Description = "Пророческа визия за тоталитарно общество...", UserId = users[1].Id },
                new Book { Title = "Властелинът на пръстените: Задругата", Author = "Дж. Р. Р. Толкин", Genre = "Фентъзи", Year = 1954, Status = "ще чета", PdfPath = "/pdfs/lotr_fellowship.pdf", Description = "Начало на легендарното пътешествие...", UserId = users[1].Id },
                new Book { Title = "Името на розата", Author = "Умберто Еко", Genre = "Детективски роман", Year = 1980, Status = "ще чета", PdfPath = "/pdfs/imeto_na_rozata.pdf", Description = "Средновековен лабиринт от тайни...", UserId = users[1].Id },

                // Елена (Id = 3)
                new Book { Title = "Дивата река", Author = "Шерил Стрейд", Genre = "Автобиографичен", Year = 2012, Status = "прочетена", PdfPath = "/pdfs/divata_reka.pdf", Description = "Разказ за самооткриване и изцеление...", UserId = users[2].Id },
                new Book { Title = "Шантарам", Author = "Грегъри Дейвид Робъртс", Genre = "Пътешествие", Year = 2003, Status = "в процес", PdfPath = "/pdfs/shantaram.pdf", Description = "Историята на бивш затворник в Бомбай...", UserId = users[2].Id },
                new Book { Title = "Кратка история на времето", Author = "Стивън Хокинг", Genre = "Научно-популярна", Year = 1988, Status = "ще чета", PdfPath = "/pdfs/brief_history_of_time.pdf", Description = "Пътешествие из тайните на космоса...", UserId = users[2].Id },
                new Book { Title = "Яж, моли се и обичай", Author = "Елизабет Гилбърт", Genre = "Мемоари", Year = 2006, Status = "прочетена", PdfPath = "/pdfs/eat_pray_love.pdf", Description = "Търсене на баланс в Италия, Индия и Бали...", UserId = users[2].Id },
                new Book { Title = "Аз още броя дните", Author = "Георги Бърдаров", Genre = "Съвременна проза", Year = 2016, Status = "ще чета", PdfPath = "/pdfs/az_oshte_broya_dnite.pdf", Description = "Емоционална история за съдби в Югославия...", UserId = users[2].Id },

                // Александър (Id = 4)
                new Book { Title = "Дюн", Author = "Франк Хърбърт", Genre = "Научна фантастика", Year = 1965, Status = "прочетена", PdfPath = "/pdfs/dune.pdf", Description = "Епична сага за пустинната планета Аракис...", UserId = users[3].Id },
                new Book { Title = "Марсианецът", Author = "Анди Уиър", Genre = "Научна фантастика", Year = 2011, Status = "в процес", PdfPath = "/pdfs/the_martian.pdf", Description = "Астронавт, изоставен на Марс...", UserId = users[3].Id },
                new Book { Title = "Аз, роботът", Author = "Айзък Азимов", Genre = "Фантастика", Year = 1950, Status = "прочетена", PdfPath = "/pdfs/i_robot.pdf", Description = "Трите закона на роботиката...", UserId = users[3].Id },
                new Book { Title = "Clean Code", Author = "Робърт Мартин", Genre = "Програмиране", Year = 2008, Status = "в процес", PdfPath = "/pdfs/clean_code.pdf", Description = "Ръководство за чист софтуерен код...", UserId = users[3].Id },
                new Book { Title = "Невромансер", Author = "Уилъм Гибсън", Genre = "Киберпънк", Year = 1984, Status = "ще чета", PdfPath = "/pdfs/neuromancer.pdf", Description = "Култово произведение за виртуалната реалност...", UserId = users[3].Id },

                // Виктория (Id = 5)
                new Book { Title = "Да убиеш присмехулник", Author = "Харпър Ли", Genre = "Класическа драма", Year = 1960, Status = "прочетена", PdfPath = "/pdfs/to_kill_a_mockingbird.pdf", Description = "Разказ за расовата несправедливост...", UserId = users[4].Id },
                new Book { Title = "Гордост и предразсъдъци", Author = "Джейн Остин", Genre = "Романтика", Year = 1813, Status = "прочетена", PdfPath = "/pdfs/pride_and_prejudice.pdf", Description = "История за класовите различия и любовта...", UserId = users[4].Id },
                new Book { Title = "Сестри Палавееви", Author = "Алек Попов", Genre = "Сатира", Year = 2013, Status = "в процес", PdfPath = "/pdfs/sestri_palaveevi.pdf", Description = "Сатиричен роман за партизанското движение...", UserId = users[4].Id },
                new Book { Title = "Метро 2033", Author = "Дмитрий Глуховски", Genre = "Пост-апокалипсис", Year = 2005, Status = "ще чета", PdfPath = "/pdfs/metro_2033.pdf", Description = "Оцеляване в московското метро...", UserId = users[4].Id },
                new Book { Title = "Приятели, любими и голямото нещо", Author = "Матю Пери", Genre = "Мемоари", Year = 2022, Status = "прочетена", PdfPath = "/pdfs/friends_lovers_and_the_big_terrible_thing.pdf", Description = "Откровена автобиография на актьора...", UserId = users[4].Id }
            };

            context.Books.AddRange(books);
            context.SaveChanges();
        }
    }
}
