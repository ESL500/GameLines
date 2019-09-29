using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace GameLines
{

    /*  состояния игры
    */
    enum GameState
    {
        WaitUserInput,      //  ожидание пользовательского клика в игровое поле
        MovingBall,         //  перемещение (анимация) шара в указанную позицию
    };


    /*  форма с игрой. здесь же реализована логика игры
    */
    public partial class FormLines : Form
    {
        //  координаты размещения игрового поля в окне формы
        const int GA_WND_X = 3;
        const int GA_WND_Y = 54;

        //  размеры игрового поля
        const int GA_WIDTH = 9;
        const int GA_HEIGHT = 9;
        const int GA_SIZE = GA_WIDTH * GA_HEIGHT;

        //  размер ячейки игрового поля
        const int GA_CELL_SIZE = 45;

        //  максимальное кол-во цветов у игровых шаров
        const int MAX_COLOR_BALL = 7;

        //  кол-во добавляемых новых шаров
        const int MAX_NEW_BALLS = 3;

        //  состояние игры
        GameState m_game_state = GameState.WaitUserInput;

        /*  игровое поле организовано одномерным массивом 9x9 элеметов; 
            в каждой ячейке содержится цвет размещенного шара. пустая ячейка без шара отмечена значением -1
        */
        int[] m_game_area;

        //  позиции и цвета новых шаров 
        int[] m_pos_new_balls;
        int[] m_clr_new_balls;

        //  индекс ячейки с выбранным шаром для перемещения; -1 - шар не выбран
        int m_sel_cell = -1;

        //  счетчик отчков
        int m_score = 0;

        //  кол-во пустых ячеек, когда переменная обнуляется, то игра заканчивается
        int m_free_cells = 0;

        //  фоновый рисунок игрового поля и кисть фонового поля
        Bitmap m_bmp_backgnd;

        //  битмэп цветных шаров. шары распологаются горизонтально с шагом в 45 пикселей
        Bitmap m_bmp_balls;

        //  битмэп цифр для отрисовки набранных отчков
        Bitmap m_bmp_digits;

        //  для генерации случайных позиций шаров и их цветов
        Random m_randomizer;

        //  таймер для анимации шаров
        Timer m_anim_timer;


        /*  
        */
        public FormLines()
        {
            InitializeComponent ();
            m_randomizer = new Random();

            //  инициализация игрового поля
            m_free_cells = GA_WIDTH * GA_HEIGHT;
            m_game_area = new int [GA_SIZE];
            for (int i = 0; i < GA_SIZE; i++)
                m_game_area[i] = -1;

            //  размещаем случайно 5 новых шаров
            for (int i = 0; i < 5; i++)
            {
                int pos = m_randomizer.Next (0, GA_SIZE);
                int clr = m_randomizer.Next (0, MAX_COLOR_BALL);
                if (m_game_area[pos] == -1)
                {
                    m_free_cells--;
                    m_game_area[pos] = clr;
                }
                else
                    i--;
            }

            //  генерим три шара, которые появятся на игровом поле после хода игрока
            m_pos_new_balls = new int[MAX_NEW_BALLS];
            m_clr_new_balls = new int[MAX_NEW_BALLS];
            for (int i = 0; i < MAX_NEW_BALLS; i++)
                m_pos_new_balls[i] = m_clr_new_balls[i] = -1;
            GenerateNextBalls ();
        }



        /*  метод для добавления новых шаров в игровое поле
        */
        private void GenerateNextBalls ()
        {
            //  переносим шары на игровое поле
            int  num_moved_balls = 0;
            for (int i = 0; i < MAX_NEW_BALLS && m_free_cells > 0; i++)
            {
                int pos = m_pos_new_balls[i];
                if (pos >= 0)
                {
                    //  если на игровом поле ячейка уже занята, то ищем новую ячейку
                    while (m_game_area[pos] >= 0)
                        pos = m_pos_new_balls[i] = m_randomizer.Next (0, GA_SIZE);

                    //  переносим шар на игровое поле
                    m_game_area[pos] = m_clr_new_balls[i];
                    num_moved_balls = i+1;
                    m_free_cells--;
                }
            }

            //  возможно с добавлением шаров, образовались комбинации для удаления
            List<int>  arr_balls = new List<int>();
            for (int i = 0; i < num_moved_balls; i++)
            {
                List<int>  arr = ValidateBalls (m_pos_new_balls[i]);
                if (arr != null && arr.Count > 0)
                    arr_balls.AddRange (arr);
            }
            if (arr_balls != null && arr_balls.Count > 0)
                RemoveBalls (arr_balls);

            //  генерим новые шары
            if (m_free_cells > 0)
            {
                for (int i = 0; i < MAX_NEW_BALLS; i++)
                {
                    int pos = 0;
                    do {
                        pos = m_randomizer.Next (0, GA_SIZE);
                    } while (m_game_area[pos] >= 0);
                
                    m_pos_new_balls[i] = pos;
                    m_clr_new_balls[i] = m_randomizer.Next(0, MAX_COLOR_BALL);
                }
            }
            else
            {
                Invalidate ();
                MessageBox.Show ("Thank you for the game", "Game over");
            }
        }



        /*  метод вызывается при загрузке формы, здесь загружаем необходимые ресурсы данной формы
        */
        private void FormLines_Load (object sender, EventArgs e)
        {
            //  загрузим фоновый рисунок игрового поля и установим размеры окна под этот рисунок
            m_bmp_backgnd = new Bitmap (@".\\res\\bg_image.bmp");
            ClientSize = new Size (m_bmp_backgnd.Size.Width, m_bmp_backgnd.Size.Height);
            
            //  загрузим рисунок цветных шаров
            m_bmp_balls = new Bitmap (@".\\res\\balls.bmp");
            m_bmp_balls.MakeTransparent (Color.White);

            //  загрузим рисунок с цифрами
            m_bmp_digits = new Bitmap (@".\\res\\digit_image.bmp");
            m_bmp_digits.MakeTransparent (Color.White);

            //  запретим ресайзинг окна пользователем
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedSingle;
        }



        /*  метод для отрисовки окна с игрой
        */
        private void Form1_Paint (object sender, PaintEventArgs e)
        {
            //  отрисовываем игровое поле
            e.Graphics.DrawImage (m_bmp_backgnd, 0, 0);

            //  отрисовываем шары на игровом поле
            for (int i = 0; i < GA_SIZE; i++)
            {
                if (m_game_area[i] >= 0 && m_game_area[i] < MAX_COLOR_BALL)
                    DrawBall (e.Graphics, i, 0, 0);
            }

            //  отрисовываем шары которые будут добавлены в игровое поле
            for (int i = 0; i < MAX_NEW_BALLS; i++)
                DrawBall (e.Graphics, m_pos_new_balls[i], 0, 0);

            //  отрисовываем очки
            DrawScore (e.Graphics, m_score);
        }



        /*  вспомогательный метод отрисовки ячейки с шаром или без. используется также при анимации шара
            ball_pos - индекс ячеки игрового поля с шаром или без
            dx, dy - смещение картинки в пикселях для анимации
        */
        private void DrawBall (Graphics g, int ball_pos, int dx, int dy)
        {
            bool  b_draw_small_ball = false;
            
            int  y = ball_pos / GA_WIDTH;
            int  x = ball_pos - y * GA_WIDTH;
            int  c = m_game_area[ball_pos];

            for (int i = 0; i < MAX_NEW_BALLS; i++)
                if (m_pos_new_balls[i] >= 0 && m_pos_new_balls[i] == ball_pos && c == -1)
                {
                    c = m_clr_new_balls[i];
                    b_draw_small_ball = true;
                }

            //  область шара и область где будем рисовать шар
            Rectangle src_rect = new Rectangle (c * 45, 0, 45, 45);
            Rectangle wnd_rect = new Rectangle (x * 45, y * 45, 45, 45);

            wnd_rect.Offset (GA_WND_X, GA_WND_Y);

            //  перезатираем фон
            g.DrawImage (m_bmp_backgnd, wnd_rect, new Rectangle(3,54,45,45), GraphicsUnit.Pixel);

            //  если шар надо отрисовать небольшим
            if (b_draw_small_ball)
                wnd_rect.Inflate (-13, -13);

            //  отрисовываем изображение шара
            wnd_rect.Offset (dx, dy);
            g.DrawImage (m_bmp_balls, wnd_rect, src_rect, GraphicsUnit.Pixel);
        }



        /*  обработчик клика мышью
        */
        private void FormLines_MouseClick (object sender, MouseEventArgs e)
        {
            //  если клик происходит в ненужное время, то ничего не делаем
            if (m_game_state != GameState.WaitUserInput || e.Button != MouseButtons.Left)
                return;

            //  если клик не в игровом поле, то также ничего не предпинимаем
            int x = (e.Location.X - GA_WND_X) / GA_CELL_SIZE;
            int y = (e.Location.Y - GA_WND_Y) / GA_CELL_SIZE;
            if (x < 0 || x >= GA_WIDTH || y < 0 || y >= GA_HEIGHT)
                return;

            //  индекс игровой ячейки в которую произошел клик
            int  cell_ind = y * GA_WIDTH + x;

            //  если клик в шар, то запоминаем данный шар как выбранный для перемещения
            if (m_game_area[cell_ind] >= 0 && cell_ind != m_sel_cell)
            {
                m_sel_cell = cell_ind;
                RunAnimationSelectedBall ();
                Invalidate (false);
            }

            //  если клик в пустое поле и ранее выбран шар, то выполняем перемещение шара
            if (m_game_area[cell_ind] == -1 && m_sel_cell >= 0)
            {
                List<int>  path = CalcMovePath (m_sel_cell, cell_ind);
                if (path != null)
                {
                    RunAnimationMoveBall (m_sel_cell, path);
                    m_sel_cell = -1;
                }
            }
        }



        /*  метод запускает анимацию для перемещения шара из одной ячейки в другую
            ball_ind_from - текущая позиция шара
            path - список индексов для перемещения шара
        */
        private void RunAnimationMoveBall (int ball_ind_from, List<int> path)
        {
            m_game_state = GameState.MovingBall;
            Timer  timer = new Timer ();
            timer.Interval = 10;

            int  ball_ind_to = path[0];
            path.RemoveAt (0);

            //  координаты шара на экране
            int  y0 = ball_ind_from / GA_WIDTH;
            int  x0 = ball_ind_from - y0 * GA_WIDTH;
            int  y1 = ball_ind_to / GA_WIDTH;
            int  x1 = ball_ind_to - y1 * GA_WIDTH;

            Graphics g = this.CreateGraphics ();
            int  dx=0;
            int  dy=0;
            int  cnt=0;
            
            timer.Tick += new EventHandler((o, ev) =>
            {
                //  если игрок выбрал другой шар, то прекращяем анимацию
                if (cnt++ >= GA_CELL_SIZE / 15)
                {
                    Timer t = o as Timer;
                    t.Stop ();

                    //  шар перемещен в соседнюю клетку, поэтому актуализируем игровое поле
                    m_game_area[ball_ind_to] = m_game_area[ball_ind_from];
                    m_game_area[ball_ind_from] = -1;
                    DrawBall (g, ball_ind_from, 0, 0);

                    //  если путь не пройден, то продолжаем перемещение шара, иначе выполняем вычисления после завершения перемещения
                    if (path.Count > 0)
                        RunAnimationMoveBall (ball_ind_to, path);
                    else
                        CompleteMoveBall (ball_ind_to);
                    return;
                }

                //  перемещаем шар и отрисовываем в новом месте
                dx += (x1 - x0) * GA_CELL_SIZE / 3;
                dy += (y1 - y0) * GA_CELL_SIZE / 3;
                
                DrawBall (g, ball_ind_to, 0, 0);
                DrawBall (g, ball_ind_from, dx, dy);
            });
            timer.Start();
        }



        /*  метод расчитывает путь перемещения шара из ячейки с индексом src_ind в ячейку с индексом dst_ind
            если путь расчитан, то возвращается массив индексов ячеек; если не расчитан, то возвращается null
        */
        private List<int> CalcMovePath (int src_ind, int dst_ind)
        {
            //  инициализируем массив для вычислений: 0 - пустая ячейка, -1 - ячейка занята
            int[] arr = new int [GA_SIZE];
            for (int i = 0; i < GA_SIZE; i++)
                arr[i] = (m_game_area[i] == -1) ? 0 : -1;

            arr[src_ind] = 1;

            //  перебираем все ячейки до тех пор пока заполяются ячейки и пока не дойдет до цели
            bool  b_found_path = false;
            while (!b_found_path)
            {
                bool  b_continue = false;

                for (int i=0; i<GA_SIZE && !b_found_path; i++)
                {
                    if (arr[i] != 0)
                        continue;

                    int y = i / GA_WIDTH;
                    int x = i - y * GA_WIDTH;

                    //  ищем соседнюю ячейку с наименьшим положительным значением
                    int new_val = 255;
                    if (x > 0 && arr[i-1] > 0 && arr[i-1] < new_val)
                        new_val = arr[i-1];
                    if (x < GA_WIDTH-1 && arr[i+1] > 0 && arr[i+1] < new_val)
                        new_val = arr[i+1];
                    if (y > 0 && arr[i-GA_WIDTH] > 0 && arr[i-GA_WIDTH] < new_val)
                        new_val = arr[i-GA_WIDTH];
                    if (y < GA_HEIGHT-1 && arr[i+GA_WIDTH] > 0 && arr[i+GA_WIDTH] < new_val)
                        new_val = arr[i+GA_WIDTH];

                    //  наименьшее положительное значение записываем в текущую ячейку
                    if (new_val > 0 && new_val < 255)
                    {
                        arr[i] = new_val + 1;
                        if (i == dst_ind)
                            b_found_path = true;
                        b_continue = true;
                    }
                }

                if (b_continue == false)
                    break;
            }


            //  если путь нашли, то заполняем список индексами для перемещения шара
            List<int>  path = null;
            if (b_found_path)
            {
                path = new List<int>();
                for (int i=dst_ind; i!=src_ind; )
                {
                    path.Insert (0, i);

                    int y = i / GA_WIDTH;
                    int x = i - y * GA_WIDTH;

                    if (x > 0 && arr[i-1] == arr[i]-1)
                        i = i - 1;
                    else if (x < GA_WIDTH-1 && arr[i+1] == arr[i]-1)
                        i = i + 1;
                    else if (y > 0 && arr[i-GA_WIDTH] == arr[i]-1)
                        i = i - GA_WIDTH;
                    else if (y < GA_HEIGHT-1 && arr[i+GA_WIDTH] == arr[i]-1)
                        i = i + GA_WIDTH;
                }
            }

            return path;
        }



        /*  метод обновляет игровую область после перемещения шара. 
        */
        private void CompleteMoveBall (int cell_ind)
        {
            //  если собрано в ряд 5 и более шаров, то убираем их с игрового поля
            List<int>  arr = ValidateBalls (cell_ind);
            if (arr != null && arr.Count > 0)
                RemoveBalls (arr);

            //  иначе добавляем на игровое поле новые шары
            else
                GenerateNextBalls ();

            //  перерисовываем игровое поле и готовы принять след. ход игрока
            Invalidate (false);
            m_game_state = GameState.WaitUserInput;
        }



        /*
        */
        private List<int> ValidateBalls (int cell_ind)
        {
            int clr = (cell_ind >= 0 && cell_ind < GA_SIZE) ? m_game_area[cell_ind] : -1;
            if (clr == -1)
                return null;

            int cell_y = cell_ind / GA_WIDTH;
            int cell_x = cell_ind - cell_y * GA_WIDTH;

            //  вычисляем кол-во одинаковых шаров слева и справа
            int l_cnt = 0, r_cnt = 0;
            for (int xl = cell_x, i = cell_ind; xl >= 0 && m_game_area[i] == clr; xl--, i--)
                l_cnt += (i != cell_ind) ? 1 : 0;
            for (int xr = cell_x, i = cell_ind; xr < GA_WIDTH && m_game_area[i] == clr; xr++, i++)
                r_cnt += (i != cell_ind) ? 1 : 0;

            //  вычисляем кол-во одинаковых шаров сверху и снизу
            int u_cnt = 0, d_cnt = 0;
            for (int yu = cell_y, i = cell_ind; yu >= 0 && m_game_area[i] == clr; yu--, i-=GA_WIDTH)
                u_cnt += (i != cell_ind) ? 1 : 0;
            for (int yd = cell_y, i = cell_ind; yd < GA_HEIGHT && m_game_area[i] == clr; yd++, i += GA_WIDTH)
                d_cnt += (i != cell_ind) ? 1 : 0;

            //  вычисляем кол-во одинаковых шаров по четырем диагоналям
            int lu_cnt = 0, ru_cnt = 0;
            for (int xl = cell_x, yu = cell_y, i = cell_ind; xl >= 0 && yu >= 0 && m_game_area[i] == clr; xl--, yu--, i += -GA_WIDTH-1)
                lu_cnt += (i != cell_ind) ? 1 : 0;
            for (int xr = cell_x, yu = cell_y, i = cell_ind; xr < GA_WIDTH && yu >= 0 && m_game_area[i] == clr; xr++, yu--, i += -GA_WIDTH+1)
                ru_cnt += (i != cell_ind) ? 1 : 0;

            int ld_cnt = 0, rd_cnt = 0;
            for (int xl = cell_x, yd = cell_y, i = cell_ind; xl >= 0 && yd < GA_HEIGHT && m_game_area[i] == clr; xl--, yd++, i += GA_WIDTH - 1)
                ld_cnt += (i != cell_ind) ? 1 : 0;
            for (int xr = cell_x, yd = cell_y, i = cell_ind; xr < GA_WIDTH && yd < GA_HEIGHT && m_game_area[i] == clr; xr++, yd++, i += GA_WIDTH + 1)
                rd_cnt += (i != cell_ind) ? 1 : 0;

            //  возвращаемый список элементов
            List<int> arr = new List<int>();

            //  если образуются ряды по 5 и более шаров, то возвращаем массив этих позиций
            if (l_cnt + r_cnt + 1 >= 5)
                for (int i = cell_ind - l_cnt; i < cell_ind + r_cnt + 1; i++)
                    arr.Add (i);

            if (u_cnt + d_cnt + 1 >= 5)
                for (int i = cell_ind - u_cnt*GA_WIDTH; i < cell_ind + (d_cnt + 1)*GA_WIDTH; i+=GA_WIDTH)
                    arr.Add (i);

            if (lu_cnt + rd_cnt + 1 >= 5)
                for (int i = cell_ind - lu_cnt * (GA_WIDTH+1); i < cell_ind + (rd_cnt + 1) * (GA_WIDTH+1); i += GA_WIDTH+1)
                    arr.Add(i);

            if (ru_cnt + ld_cnt + 1 >= 5)
                for (int i = cell_ind - ru_cnt * (GA_WIDTH-1); i < cell_ind + (ld_cnt + 1) * (GA_WIDTH-1); i += GA_WIDTH-1)
                    arr.Add(i);

            return arr;
        }



        /*  метод для удаления шаров с игрового поля и подсчета очков
        */
        private void RemoveBalls (List<int> arr)
        {
            if (arr != null)
                foreach (int pos in arr)
                    if (m_game_area[pos] >= 0)
                    {
                        m_score++;
                        m_free_cells++;
                        m_game_area[pos] = -1;
                    }
    
            //  отрисовываем набранные очки
            DrawScore (null, m_score);
        }



        /*  метод для отрисовки набранных очков
        */
        private void DrawScore (Graphics g, int score)
        {
            if (g == null)
                g = this.CreateGraphics ();

            int  delim = 10000;
            for (int i=0; i<5; i++)
            {
                int  d = score / delim;
                score -= d * delim;
                delim /= 10;

                Rectangle wnd_rect = new Rectangle (i*21+290, 7, 21, 35);
                Rectangle src_rect = new Rectangle (21*d, 0, 21, 35);
                g.DrawImage (m_bmp_digits, wnd_rect, src_rect, GraphicsUnit.Pixel);
            }
        }



        /*  метод запускает анимацию для выбранного шара
        */
        private void RunAnimationSelectedBall ()
        {
            m_anim_timer = new Timer ();
            m_anim_timer.Interval = 40;

            Graphics g = this.CreateGraphics ();
            int sel_ball = m_sel_cell;
            int count = 0;
            int dy = 0;

            m_anim_timer.Tick += new EventHandler((o, ev) =>
            {
                //  если игрок выбрал другой шар, то прекращяем анимацию
                if (sel_ball != m_sel_cell)
                {
                    Timer t = o as Timer;
                    t.Stop ();
                    return;
                }

                //  иначе отрисовываем шар в движении вверх-вниз
                dy += ((count++ % 12) < 6) ? 1 : -1;
                DrawBall (g, sel_ball, 0, dy-3);
            });
            m_anim_timer.Start();
        }
    }
}
