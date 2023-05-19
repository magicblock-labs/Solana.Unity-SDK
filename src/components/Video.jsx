


export function Video({src}) {
    const videoResponsive = {
        overflow: "hidden",
        paddingBottom: "56.25%",
        position: "relative",
        height: 0
      }
    
      const videoResponsiveIframe = {
        left: 0,
        top: 0,
        height: "100%",
        width: "100%",
        position: "absolute"
        }

    return (
      <div style={videoResponsive}>
        <iframe
        style={videoResponsiveIframe}
        width="853"
        height="480"
        src={src}
        frameborder="0"
        allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
        allowFullScreen
        title="Embedded Video"
        />
  </div>
    )
  }