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
        List<Music> MusicList = new List<Music>();
        Stack<Music> PlayLog = new Stack<Music>();

        public MainWindow()
        {
            InitializeComponent();

            SongListView.ItemsSource = MusicList;
            CheckPlaylistExits();
            CurrentMusic.MediaEnded += delegate { PlayNextSong(); };
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
        }

        public void PlaySong()
        {
            CurrentMusic.Play();
            PlayPauseButton.Content = "||";
            IsPlaying = true;
            PlayLog.Push(MusicList[CurrentMusicIndex]);
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

        // Events of controll buttons

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
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

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentMusic != null)
            {
                CurrentMusic.Stop();
                PlayPauseButton.Content = "▶";
                IsPlaying = false;
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            PlayNextSong();
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (PlayLog.Count > 0)
            {
                PlayLog.Pop();
                ChangeMusicTo(PlayLog.Peek());
                PlaySong(false);
            }
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
            if (MuteTogglButton.IsChecked == true)
            {
                CurrentMusic.IsMuted = true;
            }
            else
            {
                CurrentMusic.IsMuted = false;
            }
        }
    }

    public class Music
    {
        public string title { get; set; }
        public string path { get; set; }

        public Music(string path)
        {
            this.path = path;
            this.title = System.IO.Path.GetFileNameWithoutExtension(path);
        }
    }
}
