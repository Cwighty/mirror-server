{
  inputs.nixpkgs.url = "nixpkgs";
  outputs = { self, nixpkgs }: nixpkgs.lib.recursiveUpdate (import ./src/flake.nix) { };
}
