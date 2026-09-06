import { Icon } from "cs2/ui"

import { useTranslation } from "../utils/locale"

import { gameIconSrc } from "./icons"
import { VT } from "./vanilla"

import css from "./SelectDistrictsButton.module.scss"

// Toggles district-picking mode for a group; `selected` reflects whether it's currently active.
export const SelectDistrictsButton = (props: { selected: boolean; onSelect: () => void }) => {
    const t = useTranslation()

    return (
        <button
            className={[VT.sectionPrimaryButton.button, css.selectDistrictsButton, props.selected ? "selected" : ""]
                .filter(Boolean).join(" ")}
            onClick={props.onSelect}
        >
            <Icon className={VT.sectionPrimaryButton.icon} src={gameIconSrc("Districts")} />
            <span className={VT.sectionPrimaryButton.label}>{t("selectDistrictsButton")}</span>
        </button>
    )
}
