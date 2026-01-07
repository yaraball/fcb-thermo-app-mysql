using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace fcb_thermo_app.Views
{
  /*
   * BottomToolbar
   * UserControl for the application's bottom toolbar.
   * Handles color scale input, timeline navigation, playback, and time offset controls.
   */
  public partial class BottomToolbar : UserControl
  {
    private DispatcherTimer? _playTimer;
    private bool _isDraggingSlider = false;

    /*
     * BottomToolbar()
     * Initializes the BottomToolbar, sets up event handlers, and updates the timeline UI.
     */
    public BottomToolbar()
    {
      InitializeComponent();
      Loaded += (_, __) => UpdateTimelineUI();

      // Subscribe to DragCompleted event of the slider's Thumb
      var thumb = GetSliderThumb(TimelineSlider);
      if (thumb != null)
      {
        thumb.DragStarted += TimelineSlider_DragStarted;
        thumb.DragCompleted += TimelineSlider_DragCompleted;
      }
    }

    /*
     * UpdateColorScaleDisplay
     * Updates the color scale input fields based on current settings.
     */
    public void UpdateColorScaleDisplay()
    {
      if (Settings.IsColorScaleAuto)
      {
        MinColorScaleInput.Text = Settings.ColorScaleMin == -1 ? "N/A" : $"{Settings.ColorScaleMin}";
        MaxColorScaleInput.Text = Settings.ColorScaleMax == -1 ? "N/A" : $"{Settings.ColorScaleMax}";
      }
    }


    /*
     * MinColorScaleInput_LostFocus, MinColorScaleInput_KeyDown,
     * MaxColorScaleInput_LostFocus, MaxColorScaleInput_KeyDown
     * Handle user input for min/max color scale fields and trigger validation.
     */
    private void MinColorScaleInput_LostFocus(object sender, RoutedEventArgs e)
    {
      TryUpdateColorScale();
    }

    private void MinColorScaleInput_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        TryUpdateColorScale();
        Keyboard.ClearFocus();
      }
    }

    private void MaxColorScaleInput_LostFocus(object sender, RoutedEventArgs e)
    {
      TryUpdateColorScale();
    }

    private void MaxColorScaleInput_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        TryUpdateColorScale();
        Keyboard.ClearFocus();
      }
    }

    /*
     * TryUpdateColorScale
     * Validates and applies user input for color scale min/max values.
     * Updates settings and triggers a redraw if valid, otherwise resets input fields.
     */
    private void TryUpdateColorScale()
    {
      string minText = MinColorScaleInput.Text.Trim();
      string maxText = MaxColorScaleInput.Text.Trim();

      // Only validate if the input is different from the current settings
      if (minText == Settings.ColorScaleMin.ToString() && maxText == Settings.ColorScaleMax.ToString() ||
          (minText == "N/A" && Settings.ColorScaleMin == -1) ||
          (maxText == "N/A" && Settings.ColorScaleMax == -1))
        return;

      if (double.TryParse(minText, out double min) &&
          double.TryParse(maxText, out double max) &&
          max > min)
      {
        Settings.ColorScaleMin = min;
        Settings.ColorScaleMax = max;
        Settings.IsColorScaleAuto = false;
        AutoColorScaleToggle.IsChecked = false;
        UpdateColorScaleDisplay();
        (Application.Current.MainWindow as MainWindow)?.RedrawCanvases();
      }
      else
      {
        MessageBox.Show("Please enter valid numbers for min and max, and ensure max > min.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        // Reset to previous values
        UpdateColorScaleDisplay();
      }
    }

    /*
     * AutoColorScaleToggle_Checked, AutoColorScaleToggle_Unchecked
     * Toggles automatic color scale mode and updates the display.
     */
    private void AutoColorScaleToggle_Checked(object sender, RoutedEventArgs e)
    {
      Settings.IsColorScaleAuto = true;
      UpdateColorScaleDisplay();
    }

    private void AutoColorScaleToggle_Unchecked(object sender, RoutedEventArgs e)
    {
      Settings.IsColorScaleAuto = false;
      UpdateColorScaleDisplay();
    }

    /*
     * PlayButton_Click
     * Starts or stops timeline playback using a DispatcherTimer.
     */
    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
      if (_playTimer == null)
      {
        _playTimer = new DispatcherTimer();
        _playTimer.Tick += PlayTimer_Tick;
      }

      if (_playTimer.IsEnabled)
      {
        _playTimer.Stop();
        PlayButton.Content = "▶";
      }
      else
      {
        _playTimer.Start();
        PlayButton.Content = "⏸";
      }
    }

    /*
     * PlayTimer_Tick
     * Advances the timeline slider during playback and triggers redraws.
     */
    private void PlayTimer_Tick(object? sender, EventArgs e)
    {
      double nextValue = TimelineSlider.Value + TimeSpan.FromSeconds(Settings.SamplingIntervalSeconds).TotalSeconds;
      if (nextValue > TimelineSlider.Maximum)
      {
        _playTimer?.Stop();
        PlayButton.Content = "▶";
        return;
      }
      TimelineSlider.Value = nextValue;
      (Application.Current.MainWindow as MainWindow)?.RedrawAll();
    }


    /*
     * UpdateTimelineUI
     * Updates the timeline slider and time display based on measurement data.
     */
    public void UpdateTimelineUI()
    {
      var (count, interval) = GetMeasurementInfo();
      double maxOffset = count > 0 ? (count - 1) * interval : 0;

      TimelineSlider.Minimum = 0;
      TimelineSlider.Maximum = maxOffset;
      TimelineSlider.Value = Settings.CurrentTimeOffset.TotalSeconds;

      EndTimeText.Text = TimeSpan.FromSeconds(maxOffset).ToString(@"mm\:ss");

      if (OffsetInput != null)
        OffsetInput.Text = TimeSpan.FromSeconds(Settings.CurrentTimeOffset.TotalSeconds).ToString(@"mm\:ss");
    }

    /*
     * TimelineSlider_ValueChanged
     * Updates the current time offset and triggers redraws when the slider value changes.
     */
    private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      Settings.CurrentTimeOffset = TimeSpan.FromSeconds(e.NewValue);
      if (OffsetInput != null)
        OffsetInput.Text = TimeSpan.FromSeconds(e.NewValue).ToString(@"mm\:ss");

      // Only redraw if not dragging (i.e., single click or keyboard)
      if (!_isDraggingSlider)
      {
        (Application.Current.MainWindow as MainWindow)?.RedrawAll();
      }
    }

    /*
     * GetSliderThumb
     * Retrieves the Thumb control from a Slider for drag event handling.
     */
    private static Thumb? GetSliderThumb(Slider slider)
    {
      slider.ApplyTemplate();
      return slider.Template.FindName("PART_Track", slider) is Track track
          ? track.Thumb
          : null;
    }

    /*
     * TimelineSlider_DragStarted, TimelineSlider_DragCompleted
     * Handle drag events for the timeline slider, updating state and triggering redraws.
     */
    private void TimelineSlider_DragStarted(object sender, DragStartedEventArgs e)
    {
      _isDraggingSlider = true;
    }

    private void TimelineSlider_DragCompleted(object sender, DragCompletedEventArgs e)
    {
      _isDraggingSlider = false;
      Settings.CurrentTimeOffset = TimeSpan.FromSeconds(TimelineSlider.Value);
      if (OffsetInput != null)
        OffsetInput.Text = TimeSpan.FromSeconds(TimelineSlider.Value).ToString(@"mm\:ss");
      (Application.Current.MainWindow as MainWindow)?.RedrawAll();
    }

    /*
     * OffsetInput_LostFocus, OffsetInput_KeyDown
     * Handle user input for the time offset field and update the timeline accordingly.
     */
    private void OffsetInput_LostFocus(object sender, RoutedEventArgs e)
    {
      var (count, interval) = GetMeasurementInfo();
      double maxOffset = count > 0 ? (count - 1) * interval : 0;

      var input = OffsetInput.Text.Trim();
      if (TimeSpan.TryParseExact(input, @"mm\:ss", null, out var ts))
      {
        double seconds = ts.TotalSeconds;
        if (seconds < 0) seconds = 0;
        if (seconds > maxOffset) seconds = maxOffset;

        Settings.CurrentTimeOffset = TimeSpan.FromSeconds(seconds);
        TimelineSlider.Value = seconds;
        OffsetInput.Text = TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss");
        (Application.Current.MainWindow as MainWindow)?.RedrawAll();
      }
    }

    private void OffsetInput_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Enter)
      {
        OffsetInput_LostFocus(sender, new RoutedEventArgs());
        // Optionally move focus away so the user sees the update
        Keyboard.ClearFocus();
      }
    }

    /*
     * GetMeasurementInfo
     * Retrieves the count and interval of measurement entries for timeline calculations.
     * Returns: (count, interval)
     */
    private (int count, double interval) GetMeasurementInfo()
    {
      var entries1 = Settings.Measurements1To10 != null
          ? System.Text.Json.JsonSerializer.Deserialize<
              List<Services.TemperatureMappingService.MeasurementEntry>>(Settings.Measurements1To10.Data)
          : null;
      var entries2 = Settings.Measurements11To20 != null
          ? System.Text.Json.JsonSerializer.Deserialize<
              List<Services.TemperatureMappingService.MeasurementEntry>>(Settings.Measurements11To20.Data)
          : null;

      int count = entries1?.Count ?? 0;
      if (count == 0) count = entries2?.Count ?? 0;
      double interval = Settings.SamplingIntervalSeconds;
      return (count, interval);
    }
  }
}
