namespace BlueAndMeManager
{
  interface IPlaylistItem
  {
    EPlaylistContainmentState PlaylistContainmentState { get; }

    void UpdatePlaylistContainmentState(Playlist playlist);
  }
}
