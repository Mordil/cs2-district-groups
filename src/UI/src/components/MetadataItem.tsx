import { LocalizedNumber, Unit } from "cs2/l10n"
import { Icon, Tooltip } from "cs2/ui"

import css from "./MetadataItem.module.scss"

interface MetadataItemProps {
    icon: string
    value: number
    tooltip: string
}

// A labeled icon readout for a single numeric stat.
export const MetadataItem = ({ icon, value, tooltip }: MetadataItemProps) => (
    <Tooltip tooltip={tooltip}>
        <div className={css.metadataItem}>
            <Icon
                tinted={true}
                className={css.metadataIcon}
                src={icon} />

            <LocalizedNumber value={value} unit={Unit.Integer} />
        </div>
    </Tooltip>
)
