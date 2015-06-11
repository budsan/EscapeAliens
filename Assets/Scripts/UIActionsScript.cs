using UnityEngine;
using System.Collections;

public class UIActionsScript : MonoBehaviour {

	public HexMatrix GameLogic;
	public UnityEngine.UI.Button MoveButton;
	public UnityEngine.UI.Button AttackButton;
	public UnityEngine.UI.Button NotifyButton;
	public UnityEngine.UI.Text ActionDescription;

	void Start ()
	{
		if (MoveButton != null)
			MoveButton.onClick.AddListener(() => {
				MoveButtonClicked();
			});

		if (AttackButton != null)
			AttackButton.onClick.AddListener(() =>
			{
				AttackButtonClicked();
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

	void SetDescription(string message)
	{
		if (ActionDescription != null)
			ActionDescription.text = message;
	}

	void Update ()
	{
		if (GameLogic == null)
			return;

		int turnCount = GameLogic.TurnCount();
		int player = GameLogic.PlayerTurn();
		HexMatrix.SpecialSectorType type = GameLogic.CurrentSectorAction();

		if (MoveButton != null) {
			MoveButton.interactable = GameLogic.ValidSectorSelected() && type == HexMatrix.SpecialSectorType.None;
		}

		if (AttackButton != null) {
			AttackButton.interactable = !GameLogic.IsHumanPlaying() && GameLogic.ValidSectorSelected() && type == HexMatrix.SpecialSectorType.None;;
		}

		if (NotifyButton != null) {
			NotifyButton.interactable =
				(type == HexMatrix.SpecialSectorType.NoiseInAnySector && GameLogic.ValidSectorSelected())
				|| (type == HexMatrix.SpecialSectorType.NoiseInYourSector)
				|| (type == HexMatrix.SpecialSectorType.SilenceInAllSectors);
		}

		if (type == HexMatrix.SpecialSectorType.None)
		{
			if (GameLogic.IsHumanPlaying())
				SetDescription("Player " + player + ". Turn " + turnCount + ". HUMAN: Select a sector and move");
			else
				SetDescription("Player " + player + ". Turn " + turnCount + ". ALIEN: Select a sector and move or attack");
		}
		else
		{
			switch(type)
			{
				case HexMatrix.SpecialSectorType.NoiseInYourSector:
					SetDescription("Noise in your sector. Press Noise to end your turn.");
					break;
				case HexMatrix.SpecialSectorType.NoiseInAnySector:
					SetDescription("Noise in any sector. Select a sector and press Noise to end your turn.");
					break;
				case HexMatrix.SpecialSectorType.SilenceInAllSectors:
					SetDescription("Silence in all sectors. Press Noise to end your turn.");
					break;
			}
		}
	}
}
