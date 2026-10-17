extends Control
## Shown after the last level (with the original prototype's message).


func _ready() -> void:
	set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(MenuBackground.new())
	var dim := ColorRect.new()
	dim.color = Color(0.01, 0.03, 0.06, 0.72)
	dim.set_anchors_preset(Control.PRESET_FULL_RECT)
	add_child(dim)
	Audio.play_music("ending")
	var box := UI.vbox(18)
	box.alignment = BoxContainer.ALIGNMENT_CENTER
	var title := GlitchTitle.new()
	title.text = "SECURED"
	title.color = UiTheme.GREEN
	title.font_size = 96
	title.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	box.add_child(title)
	box.add_child(UI.label("All circuits are safe. For now.", "HeaderLabel", HORIZONTAL_ALIGNMENT_CENTER))
	var msg := UI.label("Você é Brabo!!\nNão fizemos mais fases, este é só um protótipo.\nSe quer mais, faz o pix :)", "", HORIZONTAL_ALIGNMENT_CENTER)
	msg.add_theme_color_override("font_color", UiTheme.GOLD)
	box.add_child(msg)
	box.add_child(UI.spacer(10))
	var credits := UI.label("Music: Juhani Junkala - Retro Game Music Pack (CC0)\nFonts: Silkscreen, Share Tech Mono (SIL OFL)", "DimLabel", HORIZONTAL_ALIGNMENT_CENTER)
	box.add_child(credits)
	box.add_child(UI.spacer(10))
	var back := UI.button("Main Menu", func() -> void: Transition.change_scene("res://scenes/ui/main_menu.tscn"), "BigButton")
	back.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	box.add_child(back)
	add_child(UI.centered(box))
	UI.reveal(box, 0.2)
