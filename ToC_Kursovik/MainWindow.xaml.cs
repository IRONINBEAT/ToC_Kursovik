using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ToC_Kursovik
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string currentFilePath;
        private string _lastSavedText;
        private bool _isCommand = false;
        private UndoStack _undoStack;
        private RedoStack _redoStack;

        public event PropertyChangedEventHandler? PropertyChanged;

        // Регулярное выражение для поиска ФИО (фамилия и инициалы)
        private string pattern = @"([А-ЯЁ][а-яё]{1,}(-[А-ЯЁ][а-яё]{1,})?(ов|ова|ин|ина|ий|ая|ой))\s*[А-ЯЁ]\.\s*[А-ЯЁ]\.|[А-ЯЁ]\.\s*[А-ЯЁ]\.\s*([А-ЯЁ][а-яё]{1,}(-[А-ЯЁ][а-яё]{1,})?(ов|ова|ин|ина|ий|ая|ой))";
        public UndoStack UndoStack
        {
            get => _undoStack;
            private set
            {
                _undoStack = value;
                OnPropertyChanged(nameof(UndoStack));
            }
        }

        public RedoStack RedoStack
        {
            get => _redoStack;
            private set
            {
                _redoStack = value;
                OnPropertyChanged(nameof(RedoStack));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            currentFilePath = string.Empty;
            _undoStack = new UndoStack();
            _redoStack = new RedoStack();
            DataContext = this;
            // Устанавливаем обработчик события изменения текста
            TextEditor.PreviewKeyDown += TextEditor_PreviewKeyDown;
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                RunSyntaxCheck(sender, e); // Или любой другой метод
                e.Handled = true; // Предотвращаем дальнейшую обработку
            }
        }
        // Обработчик нажатия клавиш
        private void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Проверяем, была ли нажата клавиша Enter или Tab
            if (e.Key == Key.Enter || e.Key == Key.Back || e.Key == Key.Tab || e.Key == Key.Space)
            {
                // Добавляем текущее состояние текста в стек отмены
                _undoStack.Push(TextEditor.Text);
                _redoStack.Clear(); // Очищаем стек повторов при изменении текста
            }
        }



        private void RunSyntaxCheck(object sender, RoutedEventArgs e)
        {

            ErrorBorder.BorderBrush = new SolidColorBrush(Colors.Black);
            ErrorOutput.Clear();
            Lexer lexer = new();

            string inputText = TextEditor.Text;
            var outputTokens = lexer.Tokenize(inputText);

            List<Error> errors = lexer.Errors;

            Parser parser = new Parser(outputTokens, errors);

            List<Error> parsedErrors = parser.Parse();



            if (parsedErrors.Count > 0)
            {
                ErrorBorder.BorderBrush = new SolidColorBrush(Colors.Red);
                foreach (var error in parsedErrors)
                {
                    ErrorOutput.AppendText($"Ошибка: {error.ErrorText} в строке {error.Line}, столбце {error.Column}\n");
                }

            }
        }
        private void NewFile(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentFilePath) || !string.IsNullOrEmpty(TextEditor.Text))
            {
                MessageBoxResult result = MessageBox.Show(
                    "Хотите сохранить файл перед созданием нового файла?",
                    "Подтверждение",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    SaveFile(sender, e);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            TextEditor.Clear();
            currentFilePath = string.Empty;

            // Очищаем стеки Undo и Redo
            _undoStack.Clear();
            _redoStack.Clear();
        }

        private void OpenFile(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentFilePath) || !string.IsNullOrEmpty(TextEditor.Text))
            {
                MessageBoxResult result = MessageBox.Show(
                    "Хотите сохранить файл перед открытием нового файла?",
                    "Подтверждение",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    SaveFile(sender, e);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                TextEditor.Text = File.ReadAllText(openFileDialog.FileName);
                currentFilePath = openFileDialog.FileName;

                // Очищаем стеки Undo и Redo
                _undoStack.Clear();
                _redoStack.Clear();
            }
        }

        private void SaveFile(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveFileAs(sender, e);
            }
            else
            {
                File.WriteAllText(currentFilePath, TextEditor.Text);

                // Очищаем стеки Undo и Redo после сохранения
                _undoStack.Clear();
                _redoStack.Clear();
            }
        }

        private void SaveFileAs(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt",
                DefaultExt = ".txt"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllText(saveFileDialog.FileName, TextEditor.Text);
                currentFilePath = saveFileDialog.FileName;
            }
        }

        private void ExitApp(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentFilePath) || !string.IsNullOrEmpty(TextEditor.Text))
            {

                // Спрашиваем у пользователя, хочет ли он сохранить изменения
                MessageBoxResult result = MessageBox.Show(
                    "У вас есть несохраненные изменения. Хотите сохранить файл перед выходом?",
                    "Подтверждение выхода",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Если пользователь выбрал "Да", то сохраняем файл
                    SaveFile(sender, e);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    // Если пользователь выбрал "Отмена", то отменяем выход
                    return;
                }
            }

            // Закрываем приложение
            Application.Current.Shutdown();
        }

        private void CutText(object sender, RoutedEventArgs e)
        {
            _undoStack.Push(TextEditor.Text);
            _redoStack.Clear();
            TextEditor.Cut();
        }

        private void CopyText(object sender, RoutedEventArgs e)
        {
            TextEditor.Copy();
        }

        private void PasteText(object sender, RoutedEventArgs e)
        {
            _undoStack.Push(TextEditor.Text);
            _redoStack.Clear();
            TextEditor.Paste();
        }

        private void DeleteText(object sender, RoutedEventArgs e)
        {
            _undoStack.Push(TextEditor.Text);
            _redoStack.Clear();
            TextEditor.SelectedText = string.Empty;
        }

        private void SelectAllText(object sender, RoutedEventArgs e)
        {
            TextEditor.SelectAll();
        }

        private void UndoText(object sender, RoutedEventArgs e)
        {
            if (_undoStack.Count > 0)
            {
                int caretPos = TextEditor.CaretIndex; // Запоминаем позицию курсора
                _redoStack.Push(TextEditor.Text);
                _isCommand = true;
                TextEditor.Text = _undoStack.Pop();
                TextEditor.CaretIndex = Math.Min(caretPos, TextEditor.Text.Length); // Восстанавливаем позицию курсора
                _isCommand = false;
            }
        }

        private void RedoText(object sender, RoutedEventArgs e)
        {
            if (_redoStack.Count > 0)
            {
                int caretPos = TextEditor.CaretIndex; // Запоминаем позицию курсора
                _undoStack.Push(TextEditor.Text);
                _isCommand = true;
                TextEditor.Text = _redoStack.Pop();
                TextEditor.CaretIndex = Math.Min(caretPos, TextEditor.Text.Length); // Восстанавливаем позицию курсора
                _isCommand = false;
            }
        }

        private void ShowHelp(object sender, RoutedEventArgs e)
        {

            string helpFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Help.html");

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(helpFilePath) { UseShellExecute = true });
        }

        private void AboutApp(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Синтаксический анализатор команды repeat языка NetLogo.\nРазработан в рамках курсовой работы по дисциплине" +
                "\n \"Теория формальных языков и компиляторов\". Автор: Воронин Илья, гр. АВТ-213.", "О программе");
        }

        private void MoveWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void ToggleMaximize(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.WindowState = WindowState.Normal;
            }
        }

        private void HighlightedText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Передаем фокус на TextEditor
            TextEditor.Focus();

            // Вызываем метод TextEditor_TextChanged
            TextEditor_TextChanged(sender, null);
        }

        private void TextEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateLineNumbers();
            TextEditor.Visibility = Visibility.Visible;
            HighlightedText.Visibility = Visibility.Collapsed;
            if (!_isCommand && _lastSavedText != TextEditor.Text)
            {
                _undoStack.Push(_lastSavedText);
                _redoStack.Clear();
                _lastSavedText = TextEditor.Text;
            }
        }


        private void UpdateLineNumbers()
        {
            // Получаем текст из RichTextBox
            string text = TextEditor.Text;

            // Разделяем текст по строкам
            string[] lines = text.Split('\n');

            // Создаем строку для номеров строк
            StringBuilder lineNumbers = new StringBuilder();

            // Заполняем строку номерами строк
            for (int i = 1; i <= lines.Length; i++)
            {
                lineNumbers.AppendLine(i.ToString());  // Добавляем номер строки с новой строки
            }

            // Устанавливаем номера строк в TextBlock
            LineNumbers.Text = lineNumbers.ToString();
        }

        private void ClearAll(object sender, RoutedEventArgs e)
        {
            ErrorBorder.BorderBrush = new SolidColorBrush(Colors.Black);
            _undoStack.Push(TextEditor.Text);
            _redoStack.Clear();

            TextEditor.Clear();
            ErrorOutput.Clear();
        }


        private void GetRegularExpression(object sender, RoutedEventArgs e)
        {
            TextEditor.Text = pattern;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        private void TaskSetup(object sender, RoutedEventArgs e)
        {
            TextEditor.Text = @"1. Постановка задачи

Команда repeat языка NetLogo позволяет выполнить набор команд n раз подряд.
Формат записи:
repeat число_повторений [ команда1 число_шагов команда2 число_шагов …, командаN число_шагов ]
Пример:
repeat 5 [ forward 10 right 15 left 3 back 7 ]

Справка (руководство пользователя) представлена в Приложении А.";
        }
        private void Grammar(object sender, RoutedEventArgs e)
        {
            TextEditor.Text = @"2. Грамматика

Определим грамматику команды repeat языка NetLogo G[<Начало>] в нотации Хомского с продукциями P:
<Начало> → 'repeat' <Пробел после repeat> 
<Пробел после repeat>→ ' ' <Число после repeat>
<Число после repeat>→ Ц <Число после repeat>
<Число после repeat>  → '[' <Команда>
<Команда> → 'forward' | 'right' | 'back' | 'left' <Пробел после команды>
<Пробел после команды> → ' ' <Число после команды>
<Число после команды> → Ц <Число после команды>
<Число после команды> → ' ' <Команда> | <Конец>
<Конец> → ']'
Ц → 1 | 2 | .. | 9 | 0
Следуя введенному формальному определению грамматики, представим G[<Начало>] ее составляющими:
1.	Z = <Начало>
2.	VT = { repeat '0' ... '9' '[' 'forward' 'right' 'back' 'left' ']' }
3.	VN = {<Начало>, <Пробел после repeat>, <Число после repeat>, <Команда>, <Пробел после команды>, <Число после команды>, <Конец>, Ц }";
        }
        private void GrammarClassification(object sender, RoutedEventArgs e)
        {
            TextEditor.Text = @"3. Классификация грамматики

Концепция автоматных, или регулярных, грамматик была сформулирована Ноамом Хомским в качестве одной из четырёх категорий формальных грамматик. Автоматные грамматики характеризуются наиболее строгими требованиями к структуре правил вывода, что делает их особенно подходящими для описания языков, распознаваемых конечными автоматами.
Формальное определение грамматики:
G[A]: A → aB | a | ε, a∈VT, A, B∈VN
В левой части правила может находиться только один нетерминальный символ.
В правой части — одна из трёх возможных конструкций:
•	терминальный символ, за которым следует нетерминальный (aB),
•	только терминальный символ (a),
•	пустая строка (ε).
Для этого класса однозначность и безвозвратность доказаны. Поэтому это наиболее часто используемый тип грамматик для практического приложения.
Таким образом, все продукции разработанной грамматики G[<Начало>] делают ее автоматной.";
        }
        private void MethodOfAnalizys(object sender, RoutedEventArgs e)
        {
            TextEditor.Text = @"4. Метод анализа

Грамматика G[<START>] представляет собой разновидность автоматной грамматики. Поскольку автоматные грамматики являются подклассом контекстно-свободных грамматик, для их анализа применяется метод рекурсивного спуска.
Суть этого метода заключается в том, что каждому нетерминальному символу соответствует программная функция, предназначенная для распознавания цепочки, порождаемой этим нетерминалом. Эти функции вызываются в порядке, определённом правилами грамматики, и могут рекурсивно вызывать сами себя. Поэтому для реализации этого метода необходимо выбрать язык программирования, поддерживающий рекурсивные конструкции. В качестве такого языка был выбран C#.
На рисунке 1 показана диаграмма, которая иллюстрирует список процедур и последовательность их вызова. Листинги программного кода для каждой процедуры приведены в приложении В.
";
        }
        private void IronsMethod(object sender, RoutedEventArgs e)
        {
            TextEditor.Text = @"5. Диагностика и нейтрализация ошибок

Следуя заданию курсовой работы, необходимо реализовать нейтрализацию синтаксических ошибок, используя метод Айронса.
5.1 Метод Айронса

При выявлении ошибки, когда в процессе синтаксического анализа во входной последовательности символов обнаруживается символ, не соответствующий ожидаемому, входная последовательность представляется в виде Tt, где T — ошибочный символ, а t — остальная часть входной последовательности. Процедура устранения ошибки включает следующие этапы:
1.	Выявляются незавершённые ветви дерева разбора.
2.	Создаётся множество L, которое включает в себя все оставшиеся символы незавершённых ветвей дерева разбора.
3.	Из входной последовательности символов удаляется следующий символ до тех пор, пока цепочка не примет вид Tt, такой, что U => T, где U ∈ L, то есть, пока следующий в цепочке символ T не сможет быть выведен из какого-нибудь из остаточных символов недостроенных кустов.
4.	Определяется, какая из незавершённых ветвей дерева разбора стала причиной появления символа U в множестве L (другими словами, к какой из незавершённых ветвей принадлежит символ U).
Таким образом, устанавливается соответствие между недостроенным поддеревом и оставшейся частью входной последовательности после удаления ошибочного фрагмента.

5.2 Метод Айронса для автоматной грамматики

Разрабатываемый синтаксический анализатор основывается на использовании автоматной грамматики. Алгоритм Айронса, применяемый для автоматной грамматики, характеризуется тем, что в случае возникновения синтаксической ошибки в процессе анализа, в дереве разбора всегда будет присутствовать только один незавершенный узел (см. рисунок 2).
 
Рисунок 2 – Недостроенный куст при возникновении синтаксической ошибки (выделен пунктиром)
Поскольку этот узел является единственным незавершенным, он будет ассоциироваться с оставшейся входной последовательностью символов.
Для коррекции ошибки предлагается последовательно удалять символы из входной последовательности до тех пор, пока не будет найден допустимый символ для текущего состояния анализа.


 
";
        }
        private void TestExamples(object sender, RoutedEventArgs e)
        {
            TextEditor.Text = @"6. Тестовые примеры

repeat 10 [ forward 15 back 2 right 21 ]
repeat 5 [ forward 21 ]
repeat  [ forward  ]
repeat 4/// [ forw=+=ard 5 !!!\\\ back 2 left ***15 ]
repeat 9 [ ]
repat 9 [ forard  back ]
repeat 15 [ forward 10 right 15 back 3 left 7";
        }
        private void Literature(object sender, RoutedEventArgs e)
        {
            TextEditor.Text = @"7. Литература

1.	Шорников Ю.В. Теория и практика языковых процессоров : учеб. пособие / Ю.В. Шорников. – Новосибирск: Изд-во НГТУ, 2004.
2.	Gries D. Designing Compilers for Digital Computers. New York, Jhon Wiley, 1971. 493 p.
3.	Теория формальных языков и компиляторов [Электронный ресурс] / Электрон. дан. URL: https://dispace.edu.nstu.ru/didesk/course/show/8594, свободный. Яз.рус. (дата обращения 20.03.2025).
";
        }
        private void Code(object sender, RoutedEventArgs e)
        {
            string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "code.txt");

            if (File.Exists(filePath))
            {
                TextEditor.Text = File.ReadAllText(filePath);
            }
            else
            {
                TextEditor.Text = $"Файл не найден: {filePath}";
            }
        }
    }


    // Классы для хранения стека операций Undo и Redo
    public class UndoStack : INotifyPropertyChanged
    {
        private readonly Stack<string> _stack = new Stack<string>();

        public event PropertyChangedEventHandler PropertyChanged;

        public void Push(string text)
        {
            _stack.Push(text);
            OnPropertyChanged(nameof(Count));
        }

        public string Pop()
        {
            var result = _stack.Count > 0 ? _stack.Pop() : null;
            OnPropertyChanged(nameof(Count));
            return result;
        }

        public void Clear()
        {
            _stack.Clear();
            OnPropertyChanged(nameof(Count));
        }

        public int Count => _stack.Count;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RedoStack : INotifyPropertyChanged
    {
        private readonly Stack<string> _stack = new Stack<string>();

        public event PropertyChangedEventHandler PropertyChanged;

        public void Push(string text)
        {
            _stack.Push(text);
            OnPropertyChanged(nameof(Count));
        }

        public string Pop()
        {
            var result = _stack.Count > 0 ? _stack.Pop() : null;
            OnPropertyChanged(nameof(Count));
            return result;
        }

        public void Clear()
        {
            _stack.Clear();
            OnPropertyChanged(nameof(Count));
        }

        public int Count => _stack.Count;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}