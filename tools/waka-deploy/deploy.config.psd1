@{
    # MO2 outer folder -> list of inner 7DTD mod folders to skip during deploy.
    # Use when a bundle mod redundantly ships a foundation mod that you maintain
    # as a separate standalone (and want the standalone to win, regardless of
    # modlist order).
    Exclude = @{
        'IZY-All in One Gun Pack v5.1' = @(
            '0-CustomParticleLoader'
        )
    }
}
