﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

public class UpgradeButtonHandler : MonoBehaviour {

	public Canvas renderCanvas;
	public Text text;
	public GameStats gameStats;
	public BuildingButtonHandler buildingButtonHandler;
	public Text popupText;

	public BuildingUpgrade[] upgrades;
	public UpgradeButton[] upgradeButtons;
	public GameObject[] buttonElementHolders;
	public Text[] buttonTexts;
	public Text[] popupTexts;
	public UpgradeButton upgradeButton;

	int oldTotalNonCursors = 0;

	public StringReader reader;

	public ButtonsCollection jsonToBuildingUpgradeButtons(string json) {
		return JsonUtility.FromJson<ButtonsCollection>(json);
	}

	void Start () {
		TextAsset file = (TextAsset)Resources.Load("json");
		reader = new StringReader (file.text);
		string json = reader.ReadToEnd();
		reader.Close();

		json = "{\"buttons\":" + json + "}";
		ButtonsCollection buttonsCollection = jsonToBuildingUpgradeButtons(json);
		upgrades = buttonsCollection.buttons;


		upgradeButtons = new UpgradeButton[upgrades.Length];
		buttonElementHolders = new GameObject[upgrades.Length];
		buttonTexts = new Text[upgrades.Length];
		popupTexts = new Text[upgrades.Length];

		float y = 208f;
		for (int i = 0; i < buttonElementHolders.Length; i++) {
			buttonElementHolders[i] = new GameObject();
			buttonElementHolders [i].transform.SetParent (renderCanvas.transform, false);
			buttonElementHolders[i].transform.localPosition = new Vector2(190f, y);

			upgradeButtons [i] = (UpgradeButton)Instantiate (upgradeButton, transform.position, transform.rotation);
			upgradeButtons [i].transform.SetParent (buttonElementHolders [i].transform, false);
			upgradeButtons [i].transform.localPosition = new Vector2(0f, 0f);

			buttonTexts[i] = Instantiate (text, transform.position, transform.rotation);
			buttonTexts[i].transform.SetParent (upgradeButtons [i].transform, false);
			buttonTexts[i].transform.localPosition = new Vector2(0f, 0f);
			buttonTexts [i].alignment = TextAnchor.MiddleCenter;

			popupTexts[i] = Instantiate (popupText, transform.position, transform.rotation);
			popupTexts[i].transform.SetParent (upgradeButtons [i].transform.GetChild(0).transform, false);
			popupTexts[i].rectTransform.localPosition = new Vector2(0f, 0f);

//			y -= 83.46f;
			y -= 62.4f;
		}
	}

	bool quantityMet(BuildingUpgrade upgrade) {
		bool a = upgrade.upgradeClass == "building" && buildingButtonHandler.findButtonWithName (upgrade.upgradeType).count >= Convert.ToInt64 (upgrade.quantityNeeded);

		string theThing = new Regex (" cookies").Replace (upgrade.quantityNeeded, "");
		bool b = upgrade.upgradeClass == "flavored cookies" && theThing.Length < 20 && gameStats.cookies >= Convert.ToInt64 (theThing);

		theThing = upgrade.quantityNeeded.Replace (" hand-made cookies", "");
		bool c = upgrade.upgradeClass == "clicking upgrades" && theThing.Length < 20 && gameStats.handmadeCookies >= Convert.ToInt64 (theThing);

		BuildingButton theBuilding = buildingForGrandmaType(upgrade);
		bool d = upgrade.upgradeClass == "grandma types" && theBuilding != null && theBuilding.count >= 15 && buildingButtonHandler.findButtonWithName ("grandma").count >= 1;

		bool e = upgrade.basePrice.Length < 20; // this shouldn't exist. temporary fix. the numbers get too big. also seen above.

		return (a || b || c || d) && e;
	}

	BuildingButton buildingForGrandmaType(BuildingUpgrade upgrade) {
		string result = new Regex ("(?<=15 ).+(?=s and)").Match (upgrade.quantityNeeded).Groups [0].ToString ();
		return buildingButtonHandler.findButtonWithName (result == "Factorie" ? "factory" : result.ToLower());
	}

	void Update() {
		List<BuildingUpgrade> upgradesQuantityMet = new List<BuildingUpgrade> ();
		for (int i = 0; i < upgrades.Length; i++) {
			BuildingUpgrade upgrade = upgrades[i];

			updateCursorUpgradeIfNeeded (upgrade);
			updateGrandmaTypeUpgradeIfNeeded (upgrade);
			updateClickingUpgradeIfNeeded (upgrade);

			upgrade.unlocked = quantityMet (upgrade);

			if (!upgrade.enabled && quantityMet (upgrade))
				upgradesQuantityMet.Add (upgrade);
		}

		upgradesQuantityMet = sortByPrice (upgradesQuantityMet);
		for (int i = 0; i < upgradeButtons.Length; i++) {
			buttonElementHolders[i].gameObject.SetActive(upgradesQuantityMet.Count > 0);
			if (upgradesQuantityMet.Count > 0) {
				// set the upgrade of the button to the last upgrade in the list and remove the upgrade from the list
				upgradeButtons [i].upgrade = upgradesQuantityMet [upgradesQuantityMet.Count - 1];
				upgradesQuantityMet.RemoveAt (upgradesQuantityMet.Count - 1);

				buttonTexts [i].text = upgradeButtons [i].upgrade.name;
				popupTexts [i].text = upgradeButtons [i].upgrade.name + " (" + gameStats.formatNumber (decimal.Parse(upgradeButtons[i].upgrade.basePrice), 0) + " cookies)\n" + upgradeButtons [i].upgrade.description;
				popupTexts[i].rectTransform.anchoredPosition = new Vector2(0f, 0f);

				buttonTexts [i].rectTransform.localPosition = new Vector2 (buttonTexts [i].rectTransform.localPosition.x, 15f);

				// set the color according to whether it is affordable or not
				if (gameStats.cookies >= Convert.ToInt64 (upgradeButtons [i].upgrade.basePrice)) {
					upgradeButtons [i].GetComponent<Image> ().color = new Color (0.7f, 0.7f, 0.7f);
					buttonTexts [i].color = Color.white;
				} else {
					upgradeButtons [i].GetComponent<Image> ().color = Color.gray;
					buttonTexts [i].color = new Color (0.7f, 0.7f, 0.7f);
				}
			}
		}
	}

	List<BuildingUpgrade> sortByPrice(List<BuildingUpgrade> theList) {
		return theList.OrderByDescending(upgrade => Convert.ToInt64 (upgrade.basePrice)).ToList();
	}

	void updateCursorUpgradeIfNeeded(BuildingUpgrade upgrade) {
		if (upgrade.enabled && upgrade.upgradeType == "cursor" && !(upgrade.name == "Reinforced index finger" || upgrade.name == "Carpal tunnel prevention cream" || upgrade.name == "Ambidextrous")) {
			int totalBuildings = buildingButtonHandler.getNumberOfBuildings();
			int totalCursors = buildingButtonHandler.findButtonWithName ("cursor").count;
			int totalNonCursors = totalBuildings - totalCursors;
			if (totalNonCursors != oldTotalNonCursors) {
				oldTotalNonCursors = totalNonCursors;

				string description = upgrade.description;
				string cookieGainPerNonCursorString = new Regex("(?<=\\+)(\\d|\\.)+(?= cookies)").Match(description).Groups[0].ToString();

				double cookieGainPerNonCursor = Convert.ToDouble (cookieGainPerNonCursorString);

				double cookiesGained = cookieGainPerNonCursor * totalNonCursors;

				gameStats.cookiesPerClickAddOn -= (decimal)upgrade.cookiesPerSecondAddOn;
				buildingButtonHandler.findButtonWithName ("cursor").cookiesPerSecondAddOn -= upgrade.cookiesPerSecondAddOn;

				upgrade.cookiesPerSecondAddOn = cookiesGained;

				gameStats.cookiesPerClickAddOn += (decimal)upgrade.cookiesPerSecondAddOn;
				buildingButtonHandler.findButtonWithName ("cursor").cookiesPerSecondAddOn += upgrade.cookiesPerSecondAddOn;
			}
		}
	}

	void updateGrandmaTypeUpgradeIfNeeded(BuildingUpgrade upgrade) {
		if (upgrade.enabled && upgrade.upgradeType == "grandma types") {
			string description = upgrade.description;
			BuildingButton theBuilding = buildingForGrandmaType(upgrade);
			double totalGrandmas = (double) buildingButtonHandler.findButtonWithName ("grandma").count;
			double grandmasPerPercentCpsIncrease = theBuilding.myName == "farm" ? 1.0 : double.Parse (new Regex ("(?<=per ).+(?= grandmas)").Match (description).Groups [0].ToString () + ".0");
			theBuilding.cookiesPerSecondAddOn = theBuilding.cookiesPerSecond * theBuilding.cookiesPerSecondMultiplier * 0.01 * totalGrandmas / grandmasPerPercentCpsIncrease;
		}
	}

	// yeah it's kind of odd to use the upgrade's "cookies per second add on" as a per-click add but whatever it works
	void updateClickingUpgradeIfNeeded(BuildingUpgrade upgrade) {
		if (upgrade.enabled && upgrade.upgradeType == "clicking upgrades") {
			double cookiesPerSecond = (double)gameStats.cookiesPerSecondTotal;
//			decimal cookiesPerClick = gameStats.cookiesPerClick;
			double add = 0.01 * cookiesPerSecond;
			gameStats.cookiesPerClickAddOn -= (decimal)upgrade.cookiesPerSecondAddOn;
			upgrade.cookiesPerSecondAddOn = add;
			gameStats.cookiesPerClickAddOn += (decimal)upgrade.cookiesPerSecondAddOn;
		}
	}

	public void TaskOnClick(UpgradeButton button) {
		if (Convert.ToInt64 (button.upgrade.basePrice) <= gameStats.cookies && !button.upgrade.enabled && quantityMet(button.upgrade)) {
			button.upgrade.enabled = true;
			gameStats.cookies -= Convert.ToInt64 (button.upgrade.basePrice);
			if (!(button.upgrade.upgradeType == "cursor" || button.upgrade.upgradeType == "clicking upgrades") || button.upgrade.name == "Reinforced index finger" || button.upgrade.name == "Carpal tunnel prevention cream" || button.upgrade.name == "Ambidextrous") {
				if (button.upgrade.upgradeType == "flavored cookies") {
					string description = button.upgrade.description;
					string multiplierIncrease = new Regex ("(?<=\\+)\\d+(?=%)").Match (description).Groups [0].ToString ();
					print (multiplierIncrease);
					gameStats.cookiesPerSecondMultiplier *= 1.0m + decimal.Parse (multiplierIncrease) / 100.0m;
				} else {
					buildingButtonHandler.findButtonWithName (button.upgrade.upgradeType == "grandma types" ? "grandma" : button.upgrade.upgradeType).cookiesPerSecondMultiplier *= 2;
				}
				if (button.upgrade.upgradeType == "cursor")
					gameStats.cookiesPerClickMultiplier *= 2;
			}
		}
	}
	
}


public class ButtonsCollection {
	public BuildingUpgrade[] buttons;
}