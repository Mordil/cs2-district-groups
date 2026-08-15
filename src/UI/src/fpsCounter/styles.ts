export const styles = {
    container: {
        // fixed, not absolute: the "GameTopLeft" mount point this is
        // appended to isn't full-viewport-width, so left:50% against it
        // centered within that narrow box instead of the actual screen.
        // fixed is relative to the viewport regardless of ancestor sizing.
        position: "fixed",
        top: "10rem",
        left: "50%",
        transform: "translateX(-50%)",
        color: "white",
        textAlign: "center",
        background: "rgba(0,0,0,0.6)",
        padding: "3rem 10rem",
        borderRadius: "4rem",
        fontSize: "14rem",
        fontWeight: "bold",
        pointerEvents: "none",
    } as const,
}
