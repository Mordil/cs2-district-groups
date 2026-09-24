import { ReactNode } from "react"

import { Icon, Tooltip } from "cs2/ui"

import css from "./MetadataItem.module.scss"

interface MetadataItemProps {
    icon: string
    // Already formatted, so a stat can read out as a count, a percentage or a placeholder alike
    value: ReactNode
    tooltip: ReactNode
    // Glyph-style icons have no color of their own to desaturate, so they're tinted to a flat theme color instead
    tinted?: boolean
}

// A labeled icon readout for a single stat.
export const MetadataItem = ({ icon, value, tooltip, tinted = false }: MetadataItemProps) => (
    <Tooltip tooltip={tooltip}>
        <div className={css.metadataItem}>
            <Icon
                tinted={tinted}
                className={tinted ? css.metadataIconTinted : css.metadataIcon}
                src={icon} />

            {value}
        </div>
    </Tooltip>
)
