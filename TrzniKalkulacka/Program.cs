// =====================================================================
//  Tržní kalkulačka
//  Semestrální projekt – konzolová aplikace v C#
//
//  Program pracuje s lineární poptávkou a nabídkou:
//      poptávka:  QD = a - b * P
//      nabídka:   QS = c + d * P
//  Spočítá tržní rovnováhu (PE, QE), cenovou elasticitu poptávky
//  a nabídky v bodě rovnováhy, vypíše tabulku pro různé ceny
//  (převis poptávky / nabídky) a nakreslí barevný graf do konzole.
// =====================================================================

using System.Globalization;

class Program
{
    // Rozměry grafu (počet řádků a sloupců znakové "plochy")
    const int GraphHeight = 20;
    const int GraphWidth = 50;

    // Počet řádků tabulky cen
    const int TableRows = 11;

    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        bool running = true;
        while (running)
        {
            Console.Clear();
            PrintHeader();

            Console.WriteLine("1 – Nový výpočet trhu");
            Console.WriteLine("2 – Ukázkový příklad (QD = 100 - 2P, QS = 10 + P)");
            Console.WriteLine("0 – Konec");
            Console.WriteLine();

            int choice = ReadInt("Vyberte možnost: ", 0, 2);

            switch (choice)
            {
                case 1:
                    RunCalculation(useExample: false);
                    break;
                case 2:
                    RunCalculation(useExample: true);
                    break;
                case 0:
                    running = false;
                    break;
            }
        }

        Console.WriteLine("Program ukončen. Na shledanou!");
    }

    // -----------------------------------------------------------------
    //  Hlavní výpočet: načte parametry, spočítá a vypíše vše potřebné
    // -----------------------------------------------------------------
    static void RunCalculation(bool useExample)
    {
        double a, b, c, d;

        Console.WriteLine();
        if (useExample)
        {
            a = 100; b = 2; c = 10; d = 1;
            Console.WriteLine("Použit ukázkový příklad: QD = 100 - 2P, QS = 10 + 1P");
        }
        else
        {
            Console.WriteLine("Poptávka má tvar  QD = a - b·P");
            a = ReadDouble("  Zadejte a (množství při ceně 0, 1 až 10000): ", 1, 10000);
            b = ReadDouble("  Zadejte b (o kolik klesne QD při zdražení o 1 Kč, 0,01 až 1000): ", 0.01, 1000);

            Console.WriteLine("Nabídka má tvar   QS = c + d·P");
            c = ReadDouble("  Zadejte c (-10000 až 10000): ", -10000, 10000);
            d = ReadDouble("  Zadejte d (o kolik vzroste QS při zdražení o 1 Kč, 0,01 až 1000): ", 0.01, 1000);

            // Aby rovnovážné množství vyšlo kladné, musí být c < a
            while (c >= a)
            {
                PrintColored("  Při c ≥ a by rovnovážné množství nebylo kladné, trh by nevznikl.", ConsoleColor.Red);
                c = ReadDouble($"  Zadejte c znovu (menší než {Format(a)}): ", -10000, a - 0.01);
            }
        }

        // ---------- Rovnováha ----------
        // QD = QS  =>  a - b·P = c + d·P  =>  P = (a - c) / (b + d)
        double pE = (a - c) / (b + d);
        double qE = a - b * pE;

        Console.WriteLine();
        PrintColored("=== TRŽNÍ ROVNOVÁHA ===", ConsoleColor.Cyan);
        Console.WriteLine($"Rovnovážná cena      PE = {Format(pE)} Kč");
        Console.WriteLine($"Rovnovážné množství  QE = {Format(qE)} ks");

        // ---------- Elasticita ----------
        // E = (dQ/dP) · (P / Q); u přímky je dQ/dP rovno sklonu (-b, resp. d)
        double ed = -b * pE / qE;
        double es = d * pE / qE;

        Console.WriteLine();
        PrintColored("=== ELASTICITA V BODĚ ROVNOVÁHY ===", ConsoleColor.Cyan);
        Console.WriteLine($"Cenová elasticita poptávky  ED = {Format(ed)}  → {DescribeElasticity(ed)}");
        Console.WriteLine($"Cenová elasticita nabídky   ES = {Format(es)}  → {DescribeElasticity(es)}");

        // ---------- Tabulka ----------
        double[,] table = BuildTable(a, b, c, d);
        Console.WriteLine();
        PrintColored("=== TABULKA PRO RŮZNÉ CENY ===", ConsoleColor.Cyan);
        PrintTable(table);

        // ---------- Graf ----------
        Console.WriteLine();
        PrintColored("=== GRAF ===", ConsoleColor.Cyan);
        DrawGraph(a, b, c, d, pE, qE);

        // ---------- Dotaz na konkrétní cenu (lze opakovat) ----------
        Console.WriteLine();
        string again = "a";
        while (again == "a")
        {
            double maxPrice = a / b;
            double p = ReadDouble($"Zadejte cenu, kterou chcete prověřit (0 až {Format(maxPrice)}): ", 0, maxPrice);
            CheckPrice(a, b, c, d, p, pE);

            Console.Write("Prověřit jinou cenu? (a = ano, cokoli jiného = zpět do menu): ");
            again = (Console.ReadLine() ?? "").Trim().ToLower();
        }
    }

    // -----------------------------------------------------------------
    //  Tabulka: 2D pole [řádek, sloupec]
    //    sloupec 0 = cena P, 1 = poptávané množství QD,
    //    sloupec 2 = nabízené množství QS, 3 = rozdíl QD - QS
    // -----------------------------------------------------------------
    static double[,] BuildTable(double a, double b, double c, double d)
    {
        double[,] table = new double[TableRows, 4];
        double maxPrice = a / b;                       // cena, při které QD = 0
        double step = maxPrice / (TableRows - 1);

        for (int i = 0; i < TableRows; i++)
        {
            double p = i * step;
            double qd = a - b * p;
            double qs = Math.Max(0, c + d * p);        // záporná nabídka nedává smysl → 0

            table[i, 0] = p;
            table[i, 1] = qd;
            table[i, 2] = qs;
            table[i, 3] = qd - qs;
        }
        return table;
    }

    static void PrintTable(double[,] table)
    {
        Console.WriteLine($"{"Cena P",10} | {"QD",10} | {"QS",10} | {"QD - QS",10} | Stav trhu");
        Console.WriteLine(new string('-', 70));

        for (int i = 0; i < table.GetLength(0); i++)
        {
            double diff = table[i, 3];

            // Barva řádku podle stavu trhu
            string state;
            if (Math.Abs(diff) < 0.5)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                state = "téměř rovnováha";
            }
            else if (diff > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                state = "převis poptávky (nedostatek)";
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                state = "převis nabídky (přebytek)";
            }

            Console.WriteLine($"{Format(table[i, 0]),10} | {Format(table[i, 1]),10} | {Format(table[i, 2]),10} | {Format(diff),10} | {state}");
        }
        Console.ResetColor();
    }

    // -----------------------------------------------------------------
    //  Graf: 2D pole znaků [řádek, sloupec]
    //  Svislá osa = cena P (nahoře nejvyšší), vodorovná osa = množství Q.
    //  D = poptávka, S = nabídka, E = rovnováha, | a - = osy, · = pomocné čáry
    // -----------------------------------------------------------------
    static void DrawGraph(double a, double b, double c, double d, double pE, double qE)
    {
        char[,] grid = new char[GraphHeight, GraphWidth];

        // Rozsah os: cena do bodu, kde poptávka klesne na 0; množství do max. hodnoty
        double pMax = a / b;
        double qMax = Math.Max(a, c + d * pMax);

        // 1) Vyplnit plochu mezerami
        for (int r = 0; r < GraphHeight; r++)
            for (int col = 0; col < GraphWidth; col++)
                grid[r, col] = ' ';

        // 2) Pomocné čáry k rovnováze (vodorovná u PE, svislá u QE)
        int rowE = PriceToRow(pE, pMax);
        int colE = QuantityToCol(qE, qMax);
        for (int col = 0; col < colE; col++) grid[rowE, col] = '·';
        for (int r = rowE + 1; r < GraphHeight; r++) grid[r, colE] = '·';

        // 3) Křivky – procházíme řádky (ceny) i sloupce (množství),
        //    aby čára neměla mezery u strmých i plochých přímek
        for (int r = 0; r < GraphHeight; r++)
        {
            double p = RowToPrice(r, pMax);
            PlotPoint(grid, r, a - b * p, qMax, 'D');
            PlotPoint(grid, r, c + d * p, qMax, 'S');
        }
        for (int col = 0; col < GraphWidth; col++)
        {
            double q = ColToQuantity(col, qMax);
            PlotPrice(grid, col, (a - q) / b, pMax, 'D');   // poptávka: P = (a - Q) / b
            PlotPrice(grid, col, (q - c) / d, pMax, 'S');   // nabídka:  P = (Q - c) / d
        }

        // 4) Bod rovnováhy
        grid[rowE, colE] = 'E';

        // 5) Výpis s barvami a osami
        Console.WriteLine($"   P ↑   (max {Format(pMax)} Kč)");
        for (int r = 0; r < GraphHeight; r++)
        {
            // Popisek PE na řádku rovnováhy
            Console.Write(r == rowE ? $"{"PE",4} |" : "     |");

            for (int col = 0; col < GraphWidth; col++)
            {
                char ch = grid[r, col];
                Console.ForegroundColor = ch switch
                {
                    'D' => ConsoleColor.Blue,
                    'S' => ConsoleColor.Red,
                    'E' => ConsoleColor.Green,
                    _   => ConsoleColor.DarkGray
                };
                Console.Write(ch);
            }
            Console.ResetColor();
            Console.WriteLine();
        }
        Console.WriteLine("     +" + new string('-', GraphWidth) + "→ Q");
        Console.WriteLine("      " + new string(' ', colE) + "QE");
        Console.Write("Legenda: ");
        PrintColoredInline("D", ConsoleColor.Blue); Console.Write(" = poptávka, ");
        PrintColoredInline("S", ConsoleColor.Red); Console.Write(" = nabídka, ");
        PrintColoredInline("E", ConsoleColor.Green); Console.WriteLine(" = rovnováha");
    }

    // Zapíše znak do grafu, pokud množství leží v rozsahu osy Q
    static void PlotPoint(char[,] grid, int row, double q, double qMax, char symbol)
    {
        if (q < 0 || q > qMax) return;
        int col = QuantityToCol(q, qMax);
        if (grid[row, col] == ' ' || grid[row, col] == '·') grid[row, col] = symbol;
    }

    // Zapíše znak do grafu, pokud cena leží v rozsahu osy P
    static void PlotPrice(char[,] grid, int col, double p, double pMax, char symbol)
    {
        if (p < 0 || p > pMax) return;
        int row = PriceToRow(p, pMax);
        if (grid[row, col] == ' ' || grid[row, col] == '·') grid[row, col] = symbol;
    }

    // Převody mezi hodnotami a pozicemi v poli (řádek 0 = nejvyšší cena)
    static int PriceToRow(double p, double pMax) =>
        (GraphHeight - 1) - (int)Math.Round(p / pMax * (GraphHeight - 1));

    static double RowToPrice(int row, double pMax) =>
        (double)(GraphHeight - 1 - row) / (GraphHeight - 1) * pMax;

    static int QuantityToCol(double q, double qMax) =>
        (int)Math.Round(q / qMax * (GraphWidth - 1));

    static double ColToQuantity(int col, double qMax) =>
        (double)col / (GraphWidth - 1) * qMax;

    // -----------------------------------------------------------------
    //  Co se stane při zadané ceně (např. při cenovém stropu nebo podlaze)
    // -----------------------------------------------------------------
    static void CheckPrice(double a, double b, double c, double d, double p, double pE)
    {
        double qd = a - b * p;
        double qs = Math.Max(0, c + d * p);
        double traded = Math.Min(qd, qs);   // prodá se jen tolik, kolik je na kratší straně trhu

        Console.WriteLine($"  Při ceně {Format(p)} Kč: QD = {Format(qd)} ks, QS = {Format(qs)} ks");

        if (Math.Abs(qd - qs) < 0.005)
        {
            PrintColored("  Trh je v rovnováze.", ConsoleColor.Green);
        }
        else if (qd > qs)
        {
            PrintColored($"  Cena je pod rovnovážnou ({Format(pE)} Kč) → převis poptávky {Format(qd - qs)} ks (nedostatek zboží).", ConsoleColor.Red);
        }
        else
        {
            PrintColored($"  Cena je nad rovnovážnou ({Format(pE)} Kč) → převis nabídky {Format(qs - qd)} ks (neprodané zboží).", ConsoleColor.Yellow);
        }
        Console.WriteLine($"  Skutečně se prodá {Format(traded)} ks.");
        Console.WriteLine();
    }

    // Slovní popis elasticity podle její absolutní hodnoty
    static string DescribeElasticity(double e)
    {
        double abs = Math.Abs(e);
        if (Math.Abs(abs - 1) < 0.005) return "jednotkově elastická";
        if (abs > 1) return "elastická (množství reaguje silněji než cena)";
        return "neelastická (množství reaguje slaběji než cena)";
    }

    // -----------------------------------------------------------------
    //  Bezpečné načítání vstupů (TryParse + kontrola rozsahu)
    // -----------------------------------------------------------------
    static double ReadDouble(string prompt, double min, double max)
    {
        while (true)
        {
            Console.Write(prompt);
            string input = (Console.ReadLine() ?? "").Trim().Replace(',', '.');

            // Přijímáme desetinnou čárku i tečku
            if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                && value >= min && value <= max)
            {
                return value;
            }
            PrintColored($"  Neplatný vstup. Zadejte číslo od {Format(min)} do {Format(max)}.", ConsoleColor.Red);
        }
    }

    static int ReadInt(string prompt, int min, int max)
    {
        while (true)
        {
            Console.Write(prompt);
            if (int.TryParse(Console.ReadLine(), out int value) && value >= min && value <= max)
            {
                return value;
            }
            PrintColored($"  Neplatná volba. Zadejte celé číslo od {min} do {max}.", ConsoleColor.Red);
        }
    }

    // -----------------------------------------------------------------
    //  Pomocné metody pro výpis
    // -----------------------------------------------------------------
    static string Format(double x) => x.ToString("0.##", CultureInfo.GetCultureInfo("cs-CZ"));

    static void PrintColored(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    static void PrintColoredInline(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ResetColor();
    }

    static void PrintHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==============================================");
        Console.WriteLine("            TRŽNÍ KALKULAČKA                  ");
        Console.WriteLine("   rovnováha • elasticita • tabulka • graf   ");
        Console.WriteLine("==============================================");
        Console.ResetColor();
        Console.WriteLine();
    }
}
