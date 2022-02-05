namespace BlueAndMeManager.ViewModel
{
  interface IPlaylistItem
  {
    EPlaylistContainmentState PlaylistContainmentState { get; }

    void UpdatePlaylistContainmentState(Playlist playlist);
  }
}
