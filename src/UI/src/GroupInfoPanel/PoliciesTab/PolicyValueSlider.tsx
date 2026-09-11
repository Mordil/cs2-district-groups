import { useEffect, useRef, useState } from "react"

import { LocalizedNumber, Unit } from "cs2/l10n"
import { Tooltip } from "cs2/ui"

import { VC, VF } from "../../components/vanilla"
import { kNoValue } from "../../constants"
import { PolicySlider } from "../../types"
import { useTranslation } from "../../utils/locale"

import css from "./index.module.scss"

interface PolicyValueSliderProps {
    slider: PolicySlider
    value: number
    // Set while the districts carrying the policy disagree, so the read-out says so instead of naming one.
    mixed?: boolean
    onCommit: (value: number) => void
}

// The adjustable value a policy carries, for one district or for every district of a group at once.
export const PolicyValueSlider = ({ slider, value, mixed, onCommit }: PolicyValueSliderProps) => {
    const t = useTranslation()

    /*
        A policy value is written as an event the simulation plays back, so it takes a few frames to
        come back through the binding - and one write per mouse-move would queue hundreds for a single
        drag. So the control holds the value it is moving to and lets go the moment a fresh one arrives:
        the drag stays smooth, the write lands once on release, and a change made anywhere else still
        wins as soon as it shows up. Keyboard and gamepad never drag, so those commit straight away.
    */
    const [pending, setPending] = useState<number | null>(null)
    const pendingRef = useRef(value)
    const draggingRef = useRef(false)

    useEffect(() => {
        if (draggingRef.current) {
            return
        }

        setPending(null)
    }, [value])

    const onChange = (next: number) => {
        pendingRef.current = next
        setPending(next)

        if (!draggingRef.current) {
            onCommit(next)
        }
    }

    const onDragStart = () => {
        draggingRef.current = true
    }

    const onDragEnd = () => {
        draggingRef.current = false
        onCommit(pendingRef.current)
    }

    const shown = pending ?? value
    const showMixed = mixed === true && pending === null

    const readOut = (
        <div className={css.sliderValue}>
            {showMixed ? kNoValue : <LocalizedNumber value={shown} unit={(slider.unit || Unit.Integer) as Unit} />}
        </div>
    )

    return (
        <div className={css.sliderRow}>
            <VC.Slider
                className={css.slider}
                focusKey={VF.FOCUS_DISABLED}
                value={shown}
                start={slider.min}
                end={slider.max}
                gamepadStep={slider.step}
                onChange={onChange}
                onDragStart={onDragStart}
                onDragEnd={onDragEnd}
            />

            {showMixed ? <Tooltip tooltip={t("mixedPolicyValueTooltip")}>{readOut}</Tooltip> : readOut}
        </div>
    )
}
