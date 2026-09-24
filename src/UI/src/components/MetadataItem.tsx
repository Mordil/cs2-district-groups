import { ReactNode } from "react"

import { Icon, Tooltip } from "cs2/ui"

import css from "./MetadataItem.module.scss"

interface MetadataItemProps {
    icon: string
    // Already formatted, so a stat can read out as a count, a percentage or a placeholder alike
    value: ReactNode
    tooltip: ReactNode
}

// A labeled icon readout for a single stat.
export const MetadataItem = ({ icon, value, tooltip }: MetadataItemProps) => (
    <Tooltip tooltip={tooltip}>
        <div className={css.metadataItem}>
            <Icon
                tinted={false}
                className={css.metadataIcon}
                src={icon} />

            {value}
        </div>
    </Tooltip>
)
