using System.Collections.Generic;

namespace BlueAndMeManager.ViewModel
{
  interface ITracksContainer
  {
    IEnumerable<Track> Tracks { get; }
  }
}
