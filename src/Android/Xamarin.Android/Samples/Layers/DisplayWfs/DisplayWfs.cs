// Copyright 2019 Esri.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at: http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an 
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific 
// language governing permissions and limitations under the License.

using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Ogc;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Drawing;
using Debug = System.Diagnostics.Debug;

namespace ArcGISRuntimeXamarin.Samples.DisplayWfs
{
    [Activity]
    [ArcGISRuntime.Samples.Shared.Attributes.Sample(
        "Display a WFS layer",
        "Layers",
        "Display a layer from a WFS service, requesting only features for the current extent.",
        "")]
    public class DisplayWfs : Activity
    {
        // Hold references to the UI controls.
        private MapView _myMapView;
        private ProgressBar _loadingProgressBar;

        // Hold a reference to the feature table.
        private WfsFeatureTable _featureTable;

        // Constant for the service URL and layer name.
        private const string ServiceUrl = "http://qadev000238.esri.com:8070/geoserver/ows?service=wfs&request=GetCapabilities";
        private const string LayerName = "tiger:tiger_roads";

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Title = "Display a WFS layer";

            CreateLayout();
            Initialize();
        }

        private async void Initialize()
        {
            // Create the map with topographic basemap.
            _myMapView.Map = new Map(Basemap.CreateTopographic());

            try
            {
                // Create the feature table from URI and layer name.
                _featureTable = new WfsFeatureTable(new Uri(ServiceUrl), LayerName);

                // Set the feature request mode to manual - only manual is supported at v100.5.
                _featureTable.FeatureRequestMode = FeatureRequestMode.ManualCache;

                // Set the axis order.
                _featureTable.AxisOrder = OgcAxisOrder.NoSwap;

                // Load the table.
                await _featureTable.LoadAsync();

                // Create a feature layer to visualize the WFS features.
                FeatureLayer manhattanFeatureLayer = new FeatureLayer(_featureTable);

                // Apply a renderer.
                manhattanFeatureLayer.Renderer = new SimpleRenderer(new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, Color.Red, 3));

                // Add the layer to the map.
                _myMapView.Map.OperationalLayers.Add(manhattanFeatureLayer);

                // Use the navigation completed event to populate the table with the features needed for the current extent.
                _myMapView.NavigationCompleted += MapView_NavigationCompleted;

                // Zoom to a small area within the dataset by default.
                MapPoint topLeft = new MapPoint(-73.993723, 40.799872, SpatialReferences.Wgs84);
                MapPoint bottomRight = new MapPoint( -73.943217, 40.761679, SpatialReferences.Wgs84);
                await _myMapView.SetViewpointGeometryAsync(new Envelope(topLeft, bottomRight));
            }
            catch (Exception e)
            {
                new AlertDialog.Builder(this).SetMessage(e.ToString()).SetTitle("Couldn't load sample.").Show();
                Debug.WriteLine(e);
            }
        }

        private async void MapView_NavigationCompleted(object sender, EventArgs e)
        {
            // Show the loading bar.
            _loadingProgressBar.Visibility = ViewStates.Visible;

            // Get the current extent.
            Envelope currentExtent = _myMapView.VisibleArea.Extent;

            // Create a query based on the current visible extent.
            QueryParameters visibleExtentQuery = new QueryParameters();
            visibleExtentQuery.Geometry = currentExtent;
            visibleExtentQuery.SpatialRelationship = SpatialRelationship.Intersects;

            try
            {
                // Populate the table with the query, leaving existing table entries intact.
                await _featureTable.PopulateFromServiceAsync(visibleExtentQuery, false, null);
            }
            catch (Exception exception)
            {
                new AlertDialog.Builder(this).SetMessage(e.ToString()).SetTitle("Couldn't populate table.").Show();
                Debug.WriteLine(exception);
            }
            finally
            {
                // Hide the loading bar.
                _loadingProgressBar.Visibility = ViewStates.Gone;
            }
        }

        private void CreateLayout()
        {
            // Create a new vertical layout for the app.
            var layout = new LinearLayout(this) { Orientation = Orientation.Vertical };

            // Add a help label.
            TextView helpLabel = new TextView(this);
            helpLabel.Gravity = GravityFlags.Center;
            helpLabel.Text = "Pan and zoom to see features.";
            layout.AddView(helpLabel);

            // Add a progress bar.
            _loadingProgressBar = new ProgressBar(this);
            _loadingProgressBar.Indeterminate = true;
            _loadingProgressBar.Visibility = ViewStates.Gone;
            layout.AddView(_loadingProgressBar);

            // Add the map view to the layout.
            _myMapView = new MapView();
            layout.AddView(_myMapView);

            // Show the layout in the app.
            SetContentView(layout);
        }
    }
}
