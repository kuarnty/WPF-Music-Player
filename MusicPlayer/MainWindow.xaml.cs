using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Win32;
using System.IO;
using System.Timers;

namespace MusicPlayer
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        MediaPlayer CurrentMusic = new MediaPlayer();
        int CurrentMusicIndex;
        bool IsPlaying = false;
        bool IsDragging = false;
        List<Music> MusicList = new List<Music>();
        Stack<Music> PlayLog = new Stack<Music>();
        Timer timer = new Timer();

        public MainWindow()
        {
            InitializeComponent();

            SongListView.ItemsSource = MusicList;
            CheckPlaylistExits();
            CurrentMusic.MediaEnded += delegate { PlayNextSong(); };
            timer.Interval = 100;
            timer.Elapsed += delegate { UpdateSongProgress(); };
            timer.Start();
        }

        ~MainWindow()
        {
            StreamWriter writer = new StreamWriter("./Playlist.txt");
            for (int i = 0; i < MusicList.Count(); i++)
            {
                writer.WriteLine(MusicList[i].path);
            }
            writer.Close();
        }

        public void CheckPlaylistExits()
        {
            if (File.Exists("./Playlist.txt"))
            {
                StreamReader reader = new StreamReader("./Playlist.txt");
                while (!reader.EndOfStream)
                {
                    MusicList.Add(new Music(reader.ReadLine()));
                }
                reader.Close();
            }
            else
            {
                AddMusicTroughDialog();
                StreamWriter writer = new StreamWriter("./Playlist.txt");
                for (int i = 0; i < MusicList.Count(); i++)
                {
                    writer.WriteLine(MusicList[i].path);
                }
                writer.Close();
            }
            SongListView.Items.Refresh();
        }

        public void AddMusicTroughDialog()
        {
            OpenFileDialog file = new OpenFileDialog();
            file.Multiselect = true;
            if (file.ShowDialog() == true)
            {
                for (int i = 0; i < file.FileNames.Count(); i++)
                {
                    MusicList.Add(new Music(file.FileNames[i]));
                    MediaPlayer media = new MediaPlayer();
                    media.Open(new Uri(file.FileNames[i]));
                    if (media.NaturalDuration.HasTimeSpan)
                    {
                        MusicList.Last().length = (media.NaturalDuration.TimeSpan.Minutes * 60 + media.NaturalDuration.TimeSpan.Seconds).ToString();
                    }
                }
                SongListView.Items.Refresh();
            }
        }

        public void ChangeMusicTo(Music music)
        {
            if (music != null)
            {
                CurrentMusic.Open(new Uri(music.path));
                CurrentMusicIndex = MusicList.IndexOf(music);
                SongTitleLabel.Content = music.title;
                SongListView.SelectedIndex = CurrentMusicIndex;
            }
            else
            {
                SongTitleLabel.Content = "Title";
                SongListView.SelectedIndex = -1;
            }

            if (CurrentMusic.NaturalDuration.HasTimeSpan)
            {
                SongLength.Content = CurrentMusic.NaturalDuration.TimeSpan.Minutes + " : " + CurrentMusic.NaturalDuration.TimeSpan.Seconds;
            }
        }
        public void ChangeMusicTo(int index)
        {
            if (index >= 0)
            {
                CurrentMusic.Open(new Uri(MusicList[index].path));
                CurrentMusicIndex = index;
                SongTitleLabel.Content = MusicList[index].title;
                SongListView.SelectedIndex = CurrentMusicIndex;
            }

            if (CurrentMusic.NaturalDuration.HasTimeSpan)
            {
                SongLength.Content = CurrentMusic.NaturalDuration.TimeSpan.Minutes + " : " + CurrentMusic.NaturalDuration.TimeSpan.Seconds;
            }

        }

        public void PlaySong()
        {
            CurrentMusic.Play();
            PlayPauseButton.Content = "||";
            IsPlaying = true;
            PlayLog.Push(MusicList[CurrentMusicIndex]);
            CurrentMusic.Volume = VolumeSlider.Value;
        }
        public void PlaySong(bool isLogging)
        {
            if (isLogging)
            {
                PlaySong();
            }
            // Prev 버튼을 통해 재생하거나 일시정지 후 다시 재생할 경우 PlayLog에 추가하지 않음
            else
            {
                CurrentMusic.Play();
                PlayPauseButton.Content = "||";
                IsPlaying = true;
                CurrentMusic.Volume = VolumeSlider.Value;
            }
        }

        public void PlayNextSong()
        {
            if (ShuffleToggleButton.IsChecked == true)
            {
                int index;
                Random random = new Random();
                index = random.Next(MusicList.Count - 1);
                if (index >= CurrentMusicIndex)
                {
                    index++;
                }
                ChangeMusicTo(index);
            }
            else
            {
                int index = (CurrentMusicIndex + 1) % (MusicList.Count);
                ChangeMusicTo(index);
            }
            PlaySong();
        }

        public void RemoveMusics()
        {
            if (MessageBoxResult.Yes.Equals(MessageBox.Show("선택한 노래들을 리스트에서 제거하시겠습니까?", "?", MessageBoxButton.YesNo)))
            {
                List<int> indexesToRemove = new List<int>();
                for (int i = 0; i < SongListView.SelectedItems.Count; i++)
                {
                    indexesToRemove.Add(SongListView.Items.IndexOf(SongListView.SelectedItems[i]));
                }
                indexesToRemove.Sort();

                for (int i = indexesToRemove.Count - 1; i >= 0; i--)
                {
                    if (indexesToRemove[i] == CurrentMusicIndex)
                    {
                        if (MessageBoxResult.Yes.Equals(MessageBox.Show("현재 재생중인 노래를 리스트에서 제거하시겠습니까?", "?", MessageBoxButton.YesNo)))
                        {
                            ChangeMusicTo(null);
                            MusicList.RemoveAt(indexesToRemove[i]);
                        }
                    }
                    else
                    {
                        MusicList.RemoveAt(indexesToRemove[i]);
                    }
                }
                SongListView.Items.Refresh();
            }
        }

        public void UpdateSongProgress()
        {
            Dispatcher.Invoke(new Action(delegate
            {
                if (!IsDragging)
                {
                    SongProgressLabel.Content = CurrentMusic.Position.Minutes + " : " + CurrentMusic.Position.Seconds;
                }
                else
                {
                    SongProgressLabel.Content = (int)(SongProgressSlider.Value) / 60 + " : " + (int)(SongProgressSlider.Value) % 60;
                }

                if (CurrentMusic.NaturalDuration.HasTimeSpan)
                {
                    SongProgressSlider.Maximum = CurrentMusic.NaturalDuration.TimeSpan.Minutes * 60 + CurrentMusic.NaturalDuration.TimeSpan.Seconds;
                    if (!IsDragging)
                    {
                        SongLength.Content = CurrentMusic.NaturalDuration.TimeSpan.Minutes + " : " + CurrentMusic.NaturalDuration.TimeSpan.Seconds;
                        SongProgressSlider.Value = CurrentMusic.Position.Minutes * 60 + CurrentMusic.Position.Seconds;
                        MusicList[CurrentMusicIndex].length = CurrentMusic.Position.Minutes * 60 + CurrentMusic.Position.Seconds.ToString();
                        SongListView.Items.Refresh();
                    }
                }
            }));

        }

        public void PlayPauseButtonClicked()
        {
            if (CurrentMusic != null)
            {
                if (IsPlaying)
                {
                    CurrentMusic.Pause();
                    PlayPauseButton.Content = "▶";
                    IsPlaying = false;
                }
                else
                {
                    PlaySong(false);
                }
            }
        }

        public void StopButtonClicked()
        {
            if (CurrentMusic != null)
            {
                CurrentMusic.Stop();
                PlayPauseButton.Content = "▶";
                IsPlaying = false;
            }
        }

        public void NextButtonClicked()
        {
            PlayNextSong();
        }

        public void PrevButtonClicked()
        {
            if (PlayLog.Count > 1)
            {
                PlayLog.Pop();
                ChangeMusicTo(PlayLog.Peek());
                PlaySong(false);
            }
        }

        public void MuteToggleButtonClicked()
        {
            if (MuteTogglButton.IsChecked == true)
            {
                CurrentMusic.IsMuted = true;
            }
            else
            {
                CurrentMusic.IsMuted = false;
            }
        }

        public void VolumeSliderValueChanged()
        {

        }
        // Events of controll buttons

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            PlayPauseButtonClicked();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopButtonClicked();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            NextButtonClicked();
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            PrevButtonClicked();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddMusicTroughDialog();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            RemoveMusics();
        }

        private void SongListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject obj = (DependencyObject)e.OriginalSource;
            while ((obj != null) && !(obj is ListViewItem))
            {
                obj = VisualTreeHelper.GetParent(obj);
            }
            //if it didn’t find a ListViewItem anywhere in the hierarch, it’s because the user
            //didn’t click on one. Therefore, if the variable isn’t null, run the code
            if (obj != null)
            {
                ChangeMusicTo(SongListView.SelectedIndex);
                PlaySong();
            }
        }

        private void MuteTogglButton_Click(object sender, RoutedEventArgs e)
        {
            MuteToggleButtonClicked();
        }

        private void SongProgressSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            IsDragging = true;
        }

        private void SongProgressSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (CurrentMusic.NaturalDuration.HasTimeSpan)
            {
                CurrentMusic.Position = new TimeSpan((int)(SongProgressSlider.Value) / 3600, (int)(SongProgressSlider.Value) / 60, (int)(SongProgressSlider.Value) % 60);
            }
            IsDragging = false;
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (VolumeLabel != null)
            {
                VolumeLabel.Content = (int)(VolumeSlider.Value * 100) + "%";
                CurrentMusic.Volume = VolumeSlider.Value;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            Keyboard.ClearFocus();
            switch (e.Key)
            {
                case Key.Space:
                    PlayPauseButtonClicked();
                    break;
                case Key.MediaPlayPause:
                    PlayPauseButtonClicked();
                    break;
                case Key.MediaNextTrack:
                    NextButtonClicked();
                    break;
                case Key.MediaPreviousTrack:
                    PrevButtonClicked();
                    break;
                case Key.VolumeMute:
                    MuteToggleButtonClicked();
                    break;
                case Key.Delete:
                    RemoveMusics();
                    break;
                case Key.Insert:
                    AddMusicTroughDialog();
                    break;
                case Key.S:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        ShuffleToggleButton.IsChecked = !ShuffleToggleButton.IsChecked;
                    break;
                case Key.O:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        AddMusicTroughDialog();
                    break;
                case Key.Up:
                    if (CurrentMusic.Volume >= 0.95)
                    {
                        CurrentMusic.Volume = 1;
                    }
                    else
                    {
                        CurrentMusic.Volume += 0.05;
                    }
                    break;
                case Key.Down:
                    if (CurrentMusic.Volume <= 0.05)
                    {
                        CurrentMusic.Volume = 0;
                    }
                    else
                    {
                        CurrentMusic.Volume -= 0.05;
                    }
                    break;
                case Key.Left:
                    PrevButtonClicked();
                    break;
                case Key.Right:
                    NextButtonClicked();
                    break;
                /*case Key.VolumeUp:
                    if (VolumeLabel != null)
                    {
                        if (CurrentMusic.Volume >= 0.95)
                        {
                            CurrentMusic.Volume = 1;
                        }
                        else
                        {
                            CurrentMusic.Volume += 0.05;
                        }
                        VolumeSlider.Value = CurrentMusic.Volume;
                    }
                    break;
                case Key.VolumeDown:
                    if (VolumeLabel != null)
                    {
                        if (CurrentMusic.Volume <= 0.5)
                        {
                            CurrentMusic.Volume = 0;
                        }
                        else
                        {
                            CurrentMusic.Volume -= 0.05;
                        }
                        VolumeSlider.Value = CurrentMusic.Volume;
                    }
                    break;*/
                default:
                    break;
            }
        }
    }

    public class Music
    {
        public string title { get; set; }
        public string path { get; set; }
        private int seconds;
        public string length
        {
            get
            {
                return seconds / 60 + " : " + seconds % 60;
            }
            set
            {
                seconds = Int32.Parse(value);
            }
        }

        public Music(string path)
        {
            this.path = path;
            this.title = System.IO.Path.GetFileNameWithoutExtension(path);
        }
    }
}
