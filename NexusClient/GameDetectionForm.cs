﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Nexus.Client.BackgroundTasks;
using Nexus.Client.Games;
using Nexus.Client.UI;
using Nexus.Client.UI.Controls;

namespace Nexus.Client
{
	/// <summary>
	/// The view displaying the progress of the search for installed games.
	/// </summary>
	public partial class GameDetectionForm : ManagedFontForm
	{
		private GameDetectionVM m_vmlViewModel = null;
		private List<IGameModeDescriptor> lstGameModes = null;

		#region Properties

		/// <summary>
		/// Gets or sets the view model that provides the data and operations for this view.
		/// </summary>
		/// <value>The view model that provides the data and operations for this view.</value>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		protected GameDetectionVM ViewModel
		{
			get
			{
				return m_vmlViewModel;
			}
			set
			{
				m_vmlViewModel = value;
				butOK.Enabled = false;
				m_vmlViewModel.GameDetector.GameResolved += new EventHandler<GameModeDiscoveredEventArgs>(GameDetector_GameResolved);
				m_vmlViewModel.GameDetector.DisableButOk += new EventHandler<GameModeDiscoveredEventArgs>(GameDetector_DisableButOk);
				GameModeSearchListViewItem gliGameModeItem = null;
				lstGameModes = new List<IGameModeDescriptor>(m_vmlViewModel.SupportedGameModes.RegisteredGameModes);
				for (Int32 i = 0; i < lstGameModes.Count; i++)
				{
					gliGameModeItem = new GameModeSearchListViewItem(lstGameModes[i], m_vmlViewModel.GameDetector);
					gameModeListView1.Controls.Add(gliGameModeItem);
				}

				Size szeMax = Screen.GetWorkingArea(this).Size;
				Size = new Size((Int32)(szeMax.Width * .9), (Int32)(szeMax.Height * .9));
				Int32 intWidthOffset = Size.Width - gameModeListView1.ClientSize.Width;
				Int32 intHeightOffset = Size.Height - gameModeListView1.ClientSize.Height;
				szeMax = gameModeListView1.ClientSize;
				Int32 intIdealWidthCount = (Int32)Math.Ceiling(Math.Sqrt(lstGameModes.Count));
				Int32 intWidth = 0;
				Int32 intHeigth = 0;
				do
				{
					intWidth = gameModeListView1.PreferredSize.Width / lstGameModes.Count * intIdealWidthCount;
					intHeigth = gameModeListView1.PreferredSize.Height * (Int32)Math.Ceiling((double)lstGameModes.Count / intIdealWidthCount);
				} while ((szeMax.Width < intWidth) && (--intIdealWidthCount > 0));
				if (intHeigth > szeMax.Height)
				{
					intWidthOffset += 17;
					intHeigth = szeMax.Height;
				}
				Size = new Size(intWidth + intWidthOffset, intHeigth + intHeightOffset);
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// A simple constructor that initializes the object with the given values.
		/// </summary>
		/// <param name="p_vmlGameDetection">The game detector view model.</param>
		public GameDetectionForm(GameDetectionVM p_vmlGameDetection)
		{
			InitializeComponent();
			this.lblInfo.Text = string.Format("NMM needs to know what games you have installed on your system in order to continue.\nPlease use the green 'tick' symbols next to each game to confirm their install paths.\nPress the 'Stop searching' button if you'd like to manually specify your game folders.\nPress the 'Quick startup' button if you want to proceed with just the games you've already verified.\nPlease note: NMM is only searching for installed games that the program supports and is not gathering or transmitting this data to any external service or site.");
			ViewModel = p_vmlGameDetection;
		}

		#endregion

		/// <summary>
		/// Handles the <see cref="GameDiscoverer.GameResolved"/> event of the game discoverer.
		/// </summary>
		/// <remarks>
		/// Enables the OK button.
		/// </remarks>
		/// <param name="sender">The object that raised the event.</param>
		/// <param name="e">A <see cref="TaskEndedEventArgs"/> describing the event arguments.</param>
		private void GameDetector_GameResolved(object sender, GameModeDiscoveredEventArgs e)
		{
			if (butOK.InvokeRequired)
			{
				butOK.Invoke((Action<object, GameModeDiscoveredEventArgs>)GameDetector_GameResolved, sender, e);
				return;
			}
			if (ViewModel.GameDetector.ResolvedGameModes.Count() == ViewModel.SupportedGameModes.RegisteredGameModes.Count())
				butOK.Enabled = true;
		}

		/// <summary>
		/// Handles the <see cref="GameDiscoverer.DisableButOk"/> event of the game discoverer.
		/// </summary>
		/// <remarks>
		/// Disable the OK button.
		/// </remarks>
		/// <param name="sender">The object that raised the event.</param>
		/// <param name="e">A <see cref="TaskEndedEventArgs"/> describing the event arguments.</param>
		private void GameDetector_DisableButOk(object sender, GameModeDiscoveredEventArgs e)
		{
			butOK.Enabled = false;
		}

		/// <summary>
		/// Handles the <see cref="Control.Click"/> event of the cancel button.
		/// </summary>
		/// <remarks>
		/// Confirms that the users wants to cancel the detection.
		/// </remarks>
		/// <param name="sender">The object that raised the event.</param>
		/// <param name="e">An <see cref="EventArgs"/> describing the event arguments.</param>
		private void butCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.None;
			if (MessageBox.Show(this, String.Format("Cancelling will exit {0}. Are you sure?", ViewModel.EnvironmentInfo.Settings.ModManagerName), "Confirm", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
			{
				DialogResult = DialogResult.Cancel;
				ViewModel.Cancel();
			}
		}

		/// <summary>
		/// Handles the <see cref="Control.Click"/> event of the stop searching button.
		/// </summary>
		/// <remarks>
		/// Confirms that the users wants to stop the game searching.
		/// </remarks>
		/// <param name="sender">The object that raised the event.</param>
		/// <param name="e">An <see cref="EventArgs"/> describing the event arguments.</param>
		private void butStopSearching_Click(object sender, EventArgs e)
		{
			foreach (GameModeListViewItemBase glvGameModeListView in gameModeListView1.Items)
			{
				GameModeSearchListViewItem gsvGameModeSearchItem = (GameModeSearchListViewItem)glvGameModeListView;
				if (!gsvGameModeSearchItem.GamePathFound)
					gsvGameModeSearchItem.StopSearching();
			}
		}

		/// <summary>
		/// Handles the <see cref="Control.Click"/> event of the quick startup button.
		/// </summary>
		/// <remarks>
		/// Confirms that the users wants to luanch the quick startup.
		/// </remarks>
		/// <param name="sender">The object that raised the event.</param>
		/// <param name="e">An <see cref="EventArgs"/> describing the event arguments.</param>
		private void butQuickStartup_Click(object sender, EventArgs e)
		{
			foreach (GameModeListViewItemBase glvGameModeListView in gameModeListView1.Items)
			{
				GameModeSearchListViewItem gsvGameModeSearchItem = (GameModeSearchListViewItem)glvGameModeListView;
				gsvGameModeSearchItem.StopSearching();
			}
		}

		/// <summary>
		/// Handles the <see cref="Control.Click"/> event of the ok button.
		/// </summary>
		/// <remarks>
		/// Closes the dialog.
		/// </remarks>
		/// <param name="sender">The object that raised the event.</param>
		/// <param name="e">An <see cref="EventArgs"/> describing the event arguments.</param>
		private void butOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
		}
	}
}
