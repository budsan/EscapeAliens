using UnityEngine;
using System.Collections;

public class UIActionsScript : TogglePivot {

	public HexMatrix GameLogic;
	public TogglePivot MoreActions;
	public Camera camera;
	public UnityEngine.UI.Button MoveButton;
	public UnityEngine.UI.Button AttackButton;
	public UnityEngine.UI.Button NotifyButton;
	public UnityEngine.UI.Button StartButton;
	public UnityEngine.UI.Text ActionDescription;

	override protected void Start ()
	{
		base.Start();

		if (MoveButton != null)
			MoveButton.onClick.AddListener(() => {
				MoveButtonClicked();
			});

		if (AttackButton != null)
			AttackButton.onClick.AddListener(() =>
			{
				AttackButtonClicked();
			});

		if (StartButton != null)
			StartButton.onClick.AddListener(() =>
			{
				StartButtonClicked();
			});

		if (NotifyButton != null)
			NotifyButton.onClick.AddListener(() =>
			{
				NotifyButtonClicked();
			});

		SetDescription("");
	}
	
	void MoveButtonClicked()
	{
		if (GameLogic != null)
			GameLogic.TurnMove();
	}

	void AttackButtonClicked()
	{

	}

	void NotifyButtonClicked()
	{
		if (GameLogic != null)
			GameLogic.TurnSpecialSectorMove();
	}

	void StartButtonClicked()
	{
		if (GameLogic != null)
			GameLogic.SetupNewTurn();
	}

	void SetDescription(string message)
	{
		if (ActionDescription != null)
			ActionDescription.text = message;
	}

	protected override void FixedUpdate()
	{
		base.FixedUpdate();

		if (!Toggled && camera != null)
		{
			Vector3 position = camera.transform.position;
			Vector3 target = new Vector3(0, position.y, 0);
			camera.transform.position = Vector3.Lerp(position, target, 0.1f);
		}
	}

	void Update ()
	{
		if (GameLogic == null)
			return;

		int player = GameLogic.PlayerTurn() + 1;
		Toggled = !GameLogic.NewTurn;
		if (MoreActions != null)
			MoreActions.Toggled = Toggled;

		if (!Toggled)
			SetDescription("Waiting player " + player + " for new turn.");
		else
		{
			int turnCount = GameLogic.CurrentRound();
			
			GameState.SectorState type = GameLogic.CurrentSectorState();

			if (MoveButton != null)
			{
				MoveButton.interactable = GameLogic.ValidSectorSelected() && type == GameState.SectorState.None;
			}

			if (AttackButton != null)
			{
				AttackButton.interactable = !GameLogic.IsHumanPlaying() && GameLogic.ValidSectorSelected() && type == GameState.SectorState.None; ;
			}

			if (NotifyButton != null)
			{
				NotifyButton.interactable =
					(type == GameState.SectorState.OnNoiseInAnySector && GameLogic.ValidSectorSelected())
					|| (type == GameState.SectorState.OnNoiseInYourSector)
					|| (type == GameState.SectorState.OnSilenceInAllSectors);
			}

			if (type == GameState.SectorState.None)
			{
				if (GameLogic.IsHumanPlaying())
					SetDescription("Player " + player + ". Turn " + turnCount + ". HUMAN: Select a sector and move");
				else
					SetDescription("Player " + player + ". Turn " + turnCount + ". ALIEN: Select a sector and move or attack");
			}
			else
			{
				switch (type)
				{
					case GameState.SectorState.OnNoiseInYourSector:
						SetDescription("Noise in your sector. Press Noise to end your turn.");
						break;
					case GameState.SectorState.OnNoiseInAnySector:
						SetDescription("Noise in any sector. Select a sector and press Noise to end your turn.");
						break;
					case GameState.SectorState.OnSilenceInAllSectors:
						SetDescription("Silence in all sectors. Press Noise to end your turn.");
						break;
				}
			}
		}
	}
}
