using UnityEngine;
using System;
using System.Collections.Generic;

/// Music player that organizes songs by genre and plays songs from the selected genre.
/// Attach to a GameObject with an AudioSource component.
[RequireComponent(typeof(AudioSource))]
public class GenreMusicPlayer : MonoBehaviour
{
    [Serializable]
    public class MusicGenre
    {
        public string genreName;
        public AudioClip[] songs;
    }
    
    [Header("Music Library")]
    [Tooltip("List of genres, each containing an array of songs.")]
    public List<MusicGenre> genres = new List<MusicGenre>();
    
    [Header("Playback Settings")]
    [Tooltip("Index of the currently selected genre.")]
    public int currentGenreIndex = 0;
    
    [Tooltip("If true, songs will shuffle randomly. If false, plays in order.")]
    public bool shuffleMode = false;
    
    [Tooltip("If true, automatically plays the next song when current one ends.")]
    public bool autoPlay = true;
    
    [Tooltip("If true, loops through the playlist when it reaches the end.")]
    public bool loopPlaylist = true;
    
    [Header("Volume")]
    [Range(0f, 1f)]
    public float volume = 1f;
    
    private AudioSource audioSource;
    private int currentSongIndex = 0;
    private List<int> shuffledIndices = new List<int>();
    private int shuffledPosition = 0;
    
    public string CurrentGenreName => GetCurrentGenre()?.genreName ?? "None";
    public string CurrentSongName => audioSource.clip != null ? audioSource.clip.name : "None";
    public bool IsPlaying => audioSource.isPlaying;
    
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }
    
    void Start()
    {
        audioSource.volume = volume;
        
        if (genres.Count > 0 && autoPlay)
        {
            GenerateShuffledIndices();
            PlayCurrentSong();
        }
    }
    
    void Update()
    {
        // Update volume if changed in inspector
        if (audioSource.volume != volume)
        {
            audioSource.volume = volume;
        }
        
        // Auto-play next song when current one ends
        if (autoPlay && !audioSource.isPlaying && audioSource.clip != null)
        {
            NextSong();
        }
    }
    
    /// Gets the currently selected genre.
    public MusicGenre GetCurrentGenre()
    {
        if (genres.Count == 0 || currentGenreIndex < 0 || currentGenreIndex >= genres.Count)
            return null;
        return genres[currentGenreIndex];
    }
    
    /// Sets the current genre by index and starts playing.
    public void SetGenre(int genreIndex)
    {
        if (genreIndex < 0 || genreIndex >= genres.Count)
        {
            Debug.LogWarning($"GenreMusicPlayer: Invalid genre index {genreIndex}");
            return;
        }
        
        currentGenreIndex = genreIndex;
        currentSongIndex = 0;
        shuffledPosition = 0;
        GenerateShuffledIndices();
        
        // Debug.Log($"GenreMusicPlayer: Switched to genre '{genres[currentGenreIndex].genreName}'");
        
        if (autoPlay)
        {
            PlayCurrentSong();
        }
    }
    
    /// Sets the current genre by name and starts playing.
    public void SetGenre(string genreName)
    {
        for (int i = 0; i < genres.Count; i++)
        {
            if (genres[i].genreName.Equals(genreName, StringComparison.OrdinalIgnoreCase))
            {
                SetGenre(i);
                return;
            }
        }
        Debug.LogWarning($"GenreMusicPlayer: Genre '{genreName}' not found");
    }
        
    /// Plays the current song.
    public void PlayCurrentSong()
    {
        MusicGenre genre = GetCurrentGenre();
        if (genre == null || genre.songs == null || genre.songs.Length == 0)
        {
            Debug.LogWarning("GenreMusicPlayer: No songs in current genre");
            return;
        }
        
        int songIndex = shuffleMode ? GetShuffledSongIndex() : currentSongIndex;
        
        if (songIndex < 0 || songIndex >= genre.songs.Length)
        {
            songIndex = 0;
            currentSongIndex = 0;
        }
        
        AudioClip clip = genre.songs[songIndex];
        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
            // Debug.Log($"GenreMusicPlayer: Playing '{clip.name}' from genre '{genre.genreName}'");
        }
    }
    
    /// Plays the next song in the playlist.
    public void NextSong()
    {
        MusicGenre genre = GetCurrentGenre();
        if (genre == null || genre.songs == null || genre.songs.Length == 0) return;
        
        if (shuffleMode)
        {
            shuffledPosition++;
            if (shuffledPosition >= shuffledIndices.Count)
            {
                if (loopPlaylist)
                {
                    GenerateShuffledIndices();
                    shuffledPosition = 0;
                }
                else
                {
                    Stop();
                    return;
                }
            }
        }
        else
        {
            currentSongIndex++;
            if (currentSongIndex >= genre.songs.Length)
            {
                if (loopPlaylist)
                {
                    currentSongIndex = 0;
                }
                else
                {
                    Stop();
                    return;
                }
            }
        }
        
        PlayCurrentSong();
    }
    
    /// Plays the previous song in the playlist.
    public void PreviousSong()
    {
        MusicGenre genre = GetCurrentGenre();
        if (genre == null || genre.songs == null || genre.songs.Length == 0) return;
        
        if (shuffleMode)
        {
            shuffledPosition--;
            if (shuffledPosition < 0)
            {
                shuffledPosition = shuffledIndices.Count - 1;
            }
        }
        else
        {
            currentSongIndex--;
            if (currentSongIndex < 0)
            {
                currentSongIndex = genre.songs.Length - 1;
            }
        }
        
        PlayCurrentSong();
    }
    
    /// Pauses the current song.
    public void Pause()
    {
        audioSource.Pause();
    }
    
    /// Resumes the paused song.
    public void Resume()
    {
        audioSource.UnPause();
    }
    
    /// Stops playback.
    public void Stop()
    {
        audioSource.Stop();
    }
    
    /// Toggles play/pause.
    public void TogglePlayPause()
    {
        if (audioSource.isPlaying)
        {
            Pause();
        }
        else if (audioSource.clip != null)
        {
            Resume();
        }
        else
        {
            PlayCurrentSong();
        }
    }
    
    /// Sets the volume (0-1).
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        audioSource.volume = volume;
    }
    
    /// Toggles shuffle mode.
    public void ToggleShuffle()
    {
        shuffleMode = !shuffleMode;
        if (shuffleMode)
        {
            GenerateShuffledIndices();
        }
        Debug.Log($"GenreMusicPlayer: Shuffle mode {(shuffleMode ? "ON" : "OFF")}");
    }

    /// Gets all genre names.
    public string[] GetGenreNames()
    {
        string[] names = new string[genres.Count];
        for (int i = 0; i < genres.Count; i++)
        {
            names[i] = genres[i].genreName;
        }
        return names;
    }
    
    private int GetShuffledSongIndex()
    {
        if (shuffledIndices.Count == 0) return 0;
        if (shuffledPosition < 0 || shuffledPosition >= shuffledIndices.Count)
            shuffledPosition = 0;
        return shuffledIndices[shuffledPosition];
    }
        
    private void GenerateShuffledIndices()
    {
        shuffledIndices.Clear();
        MusicGenre genre = GetCurrentGenre();
        if (genre == null || genre.songs == null) return;
        
        // Add all indices
        for (int i = 0; i < genre.songs.Length; i++)
        {
            shuffledIndices.Add(i);
        }
        
        // Fisher-Yates shuffle
        for (int i = shuffledIndices.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            int temp = shuffledIndices[i];
            shuffledIndices[i] = shuffledIndices[j];
            shuffledIndices[j] = temp;
        }
        
        shuffledPosition = 0;
    }
}
