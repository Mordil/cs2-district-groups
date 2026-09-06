import { MouseEvent } from "react"

import { useValue } from "cs2/api"
import { camera } from "cs2/bindings"
import { InputActionConsumer } from "cs2/input"
import { LocalizedString } from "cs2/l10n"
import { Scrollable, Tooltip } from "cs2/ui"
import { Entity, entityEquals, entityKey } from "cs2/utils"

import { serviceBuildings$ } from "../bindings"
import { gameIconSrc, modIconSrc } from "../components/icons"
import { MetadataItem } from "../components/MetadataItem"
import { VC, VF, VT } from "../components/vanilla"
import { kPanelWidth } from "../constants"
import { colorToCss } from "../shared"
import { Group } from "../types"
import { VanillaLocale, useTranslation } from "../utils/locale"
import { logger } from "../utils/log"
import { useEnterExitPhase } from "../utils/useEnterExitPhase"

import css from "./index.module.scss"

const kFadeDurationMs = 150

const stopMouseDown = (e: MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()
}

const focusTooltip = (
    <LocalizedString id={VanillaLocale.focusTooltip.id} fallback={VanillaLocale.focusTooltip.fallback} />
)

interface EntityRowProps {
    entity: Entity
    name: string
}

const EntityRow = ({ entity, name }: EntityRowProps) => (
    <div className={css.listItem}>
        <span className={css.listItemName}>{name}</span>
    </div>
)

interface GroupInfoPanelProps {
    group: Group
    onClose: () => void
}

// Read-only detail view for a single district group.
export const GroupInfoPanel = ({ group, onClose }: GroupInfoPanelProps) => {
    const t = useTranslation()
    const serviceBuildings = useValue(serviceBuildings$)
    const { phase } = useEnterExitPhase(true, kFadeDurationMs, { skipInitial: false })

    const assignedBuildings = serviceBuildings.filter((b) => entityEquals(b.assignedGroup, group.entity))

    return (
        <InputActionConsumer actions={{ Close: onClose, Back: onClose }} ignoreFocusState={true}>
            <div className={`${css.panel} ${css[phase]}`} style={{ left: `${kPanelWidth + 16}rem`, width: `${kPanelWidth}rem` }}>
                <div className={css.header}>
                    <div className={css.titleRow}>
                        <span className={css.colorSwatch} style={{ background: colorToCss(group.color) }} />
                        <span className={css.title}>{group.name}</span>

                        <VC.IconButton
                            tinted={true}
                            focusKey={VF.FOCUS_DISABLED}
                            src={VT.panel.closeIcon}
                            theme={VT.roundIconButton}
                            className={VT.panel.closeButton}
                            onSelect={onClose}
                            onMouseDown={stopMouseDown}
                        />
                    </div>
                </div>

                <div className={css.content}>
                    <div className={css.actionSection}>
                        <MetadataItem
                            icon={gameIconSrc("LotTool")}
                            value={group.members.length}
                            tooltip={t("metadataDistrictsTooltip")}
                        />
                        <MetadataItem
                            icon={modIconSrc("building")}
                            value={group.assignedBuildingCount}
                            tooltip={t("metadataBuildingsTooltip")}
                        />
                        <MetadataItem
                            icon={gameIconSrc("Population")}
                            value={group.population}
                            tooltip={t("metadataPopulationTooltip")}
                        />
                    </div>

                    <Scrollable vertical={true} trackVisibility="reserve" className={css.scrollableContent}>
                        <div className={css.listSectionHeader}>{t("metadataDistrictsTooltip")}</div>

                        {group.members.map((member) => (
                            <EntityRow key={entityKey(member.entity)} entity={member.entity} name={member.name} />
                        ))}

                        <div className={css.listSectionHeader}>{t("metadataBuildingsTooltip")}</div>

                        {assignedBuildings.map((building) => (
                            <EntityRow key={entityKey(building.entity)} entity={building.entity} name={building.name} />
                        ))}
                    </Scrollable>
                </div>
            </div>
        </InputActionConsumer>
    )
}
